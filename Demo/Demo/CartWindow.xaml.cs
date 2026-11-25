using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ChudoObuv.Models;

namespace ChudoObuv
{
    public partial class CartWindow : Window
    {
        public User? CurrentUser { get; set; }

        public CartWindow(User? currentUser)
        {
            InitializeComponent();
            CurrentUser = currentUser;

            SetupUserInfo();
            LoadCustomers();
            RefreshCartGrid();
        }

        private void SetupUserInfo()
        {
            if (CurrentUser != null)
            {
                string roleName = CurrentUser.Role?.RoleName ?? "Пользователь";
                UserInfoTextBlock.Text = CurrentUser.LastName + " " + CurrentUser.Name + " " + CurrentUser.Surname + " (" + roleName + ")";
            }
            else
            {
                UserInfoTextBlock.Text = "Гость";
            }
        }

        private void LoadCustomers()
        {
            if (CurrentUser != null && (CurrentUser.RoleId == 3 || CurrentUser.RoleId == 4))
            {
                DemoDbContext db = new DemoDbContext();
                List<User> customers = db.Users.OrderBy(u => u.LastName).ThenBy(u => u.Name).ToList();

                CustomerComboBox.ItemsSource = customers;
                CustomerComboBox.DisplayMemberPath = "FullNameWithRole";
                CustomerComboBox.SelectedItem = customers.FirstOrDefault(u => u.UserId == CurrentUser.UserId) ?? customers.FirstOrDefault();
                CustomerComboBox.Visibility = Visibility.Visible;
                ClientStaticTextBlock.Visibility = Visibility.Collapsed;
            }
            else if (CurrentUser != null)
            {
                CustomerComboBox.Visibility = Visibility.Collapsed;
                ClientStaticTextBlock.Text = CurrentUser.LastName + " " + CurrentUser.Name + " " + CurrentUser.Surname;
                ClientStaticTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void RefreshCartGrid()
        {
            List<CartItemDisplay> displayItems = new List<CartItemDisplay>();

            foreach (var item in OrderCart.Items)
            {
                CartItemDisplay row = new CartItemDisplay
                {
                    SourceItem = item,
                    TitleWithManufacturer = item.Manufacturer + " | " + item.Title,
                    SizeDisplay = item.SizeName.ToString("0.#"),
                    Quantity = item.Quantity,
                    UnitPriceDisplay = item.UnitPrice.ToString("N2") + " руб.",
                    TotalPriceDisplay = item.TotalPrice.ToString("N2") + " руб."
                };

                displayItems.Add(row);
            }

            CartItemsDataGrid.ItemsSource = null;
            CartItemsDataGrid.ItemsSource = displayItems;
            TotalSumTextBlock.Text = OrderCart.TotalSum.ToString("N2") + " руб.";

            ConfirmOrderButton.IsEnabled = OrderCart.Items.Count > 0;
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is CartItemDisplay displayItem)
            {
                OrderCart.Items.Remove(displayItem.SourceItem);
                RefreshCartGrid();
            }
        }

        private void ConfirmOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderCart.Items.Count == 0)
            {
                MessageBox.Show("Корзина пуста. Добавьте товары из каталога для оформления заказа.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int targetUserId;

            if (CurrentUser != null && (CurrentUser.RoleId == 3 || CurrentUser.RoleId == 4))
            {
                if (CustomerComboBox.SelectedItem is not User selectedCustomer)
                {
                    MessageBox.Show("Пожалуйста, выберите клиента для оформления заказа.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                targetUserId = selectedCustomer.UserId;
            }
            else if (CurrentUser != null)
            {
                targetUserId = CurrentUser.UserId;
            }
            else
            {
                MessageBox.Show("Для оформления заказа необходимо авторизоваться.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                DemoDbContext db = new DemoDbContext();

                int newOrderNumber = (db.Orders.Max(o => (int?)o.OrderNumber) ?? 0) + 1;

                Order newOrder = new Order
                {
                    OrderNumber = newOrderNumber,
                    OrderDate = DateOnly.FromDateTime(DateTime.Today),
                    UserId = targetUserId
                };
                db.Orders.Add(newOrder);

                int currentOrderItemId = (db.OrderItems.Max(oi => (int?)oi.OrderItemId) ?? 0) + 1;

                foreach (var cartItem in OrderCart.Items)
                {
                    OrderItem orderItem = new OrderItem
                    {
                        OrderItemId = currentOrderItemId++,
                        OrderNumber = newOrderNumber,
                        ProductId = cartItem.ProductId,
                        SizeId = cartItem.SizeId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.UnitPrice
                    };
                    db.OrderItems.Add(orderItem);

                    var stockItem = db.StockItems.FirstOrDefault(s => s.ProductId == cartItem.ProductId && s.SizeId == cartItem.SizeId);
                    if (stockItem != null)
                    {
                        stockItem.QuantityForOrders = Math.Max(0, stockItem.QuantityForOrders - cartItem.Quantity);
                    }
                }

                db.SaveChanges();

                decimal total = OrderCart.TotalSum;
                OrderCart.Clear();

                MessageBox.Show("Заказ №" + newOrderNumber + " успешно оформлен на сумму " + total.ToString("N2") + " руб.!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при сохранении заказа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderCart.Items.Count > 0)
            {
                var result = MessageBox.Show("Вы действительно хотите отказаться от заказа? Все выбранные позиции будут очищены.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    OrderCart.Clear();
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string Manufacturer { get; set; }
        public int SizeId { get; set; }
        public decimal SizeName { get; set; }
        public short Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public int AvailableStock { get; set; }
    }

    public static class OrderCart
    {
        public static List<CartItem> Items { get; } = new List<CartItem>();

        public static void Clear()
        {
            Items.Clear();
        }

        public static int TotalCount
        {
            get
            {
                int count = 0;
                foreach (var item in Items)
                {
                    count += item.Quantity;
                }
                return count;
            }
        }

        public static decimal TotalSum
        {
            get
            {
                decimal sum = 0;
                foreach (var item in Items)
                {
                    sum += item.TotalPrice;
                }
                return sum;
            }
        }
    }

    public class CartItemDisplay
    {
        public CartItem SourceItem { get; set; }
        public string TitleWithManufacturer { get; set; }
        public string SizeDisplay { get; set; }
        public short Quantity { get; set; }
        public string UnitPriceDisplay { get; set; }
        public string TotalPriceDisplay { get; set; }
    }
}
