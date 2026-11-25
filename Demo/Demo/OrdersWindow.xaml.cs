using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ChudoObuv.Models;

namespace ChudoObuv
{
    public partial class OrdersWindow : Window
    {
        public User? CurrentUser { get; set; }

        List<OrderRowView> _orders = new List<OrderRowView>();
        List<OrderRowView> _allOrders = new List<OrderRowView>();
        private OrderRowView? _selectedOrderView;

        public OrdersWindow(User? currentUser)
        {
            InitializeComponent();
            CurrentUser = currentUser;

            SetupInterfaceByRole();
            LoadOrders();
        }

        private void SetupInterfaceByRole()
        {
            if (CurrentUser == null)
            {
                UserInfoTextBlock.Text = "Гость";
                AdminDateEditPanel.Visibility = Visibility.Collapsed;
                DeleteOrderItemColumn.Visibility = Visibility.Collapsed;
                AddNewOrderButton.Visibility = Visibility.Collapsed;
                DeleteOrderButton.Visibility = Visibility.Collapsed;
                return;
            }

            string roleName = CurrentUser.Role?.RoleName ?? "Пользователь";
            UserInfoTextBlock.Text = CurrentUser.LastName + " " + CurrentUser.Name + " " + CurrentUser.Surname + " (" + roleName + ")";

            if (CurrentUser.RoleId == 4)
            {
                WindowTitleTextBlock.Text = "Чудо Обувь - Управление заказами (Администратор)";
                AdminDateEditPanel.Visibility = Visibility.Visible;
                DeleteOrderItemColumn.Visibility = Visibility.Visible;
                AddNewOrderButton.Visibility = Visibility.Visible;
                DeleteOrderButton.Visibility = Visibility.Visible;
            }
            else if (CurrentUser.RoleId == 3)
            {
                WindowTitleTextBlock.Text = "Чудо Обувь - Управление заказами (Менеджер)";
                AdminDateEditPanel.Visibility = Visibility.Collapsed;
                DeleteOrderItemColumn.Visibility = Visibility.Collapsed;
                AddNewOrderButton.Visibility = Visibility.Visible;
                DeleteOrderButton.Visibility = Visibility.Visible;
            }
            else
            {
                WindowTitleTextBlock.Text = "Чудо Обувь - Мои заказы";
                AdminDateEditPanel.Visibility = Visibility.Collapsed;
                DeleteOrderItemColumn.Visibility = Visibility.Collapsed;
                AddNewOrderButton.Visibility = Visibility.Collapsed;
                DeleteOrderButton.Visibility = Visibility.Collapsed;
            }
        }

        public void LoadOrders()
        {
            _allOrders.Clear();

            DemoDbContext db = new DemoDbContext();

            var query = db.Orders.Include(o => o.User).Include(o => o.OrderItems).AsQueryable();

            if (CurrentUser != null && CurrentUser.RoleId != 3 && CurrentUser.RoleId != 4)
            {
                query = query.Where(o => o.UserId == CurrentUser.UserId);
            }

            List<Order> orders = query.OrderByDescending(o => o.OrderNumber).ToList();

            foreach (var o in orders)
            {
                decimal totalSum = o.OrderItems.Sum(oi => oi.Quantity * oi.Price);
                OrderRowView view = new OrderRowView
                {
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    OrderDateDisplay = o.OrderDate.ToString("dd.MM.yyyy"),
                    ClientFullName = o.User != null ? o.User.LastName + " " + o.User.Name + " " + o.User.Surname : "Не указан",
                    TotalSum = totalSum,
                    TotalSumDisplay = totalSum.ToString("N2") + " руб.",
                    OrderEntity = o
                };

                _allOrders.Add(view);
            }

            ApplyOrdersSearch();

            if (OrdersDataGrid.Items.Count > 0)
            {
                OrdersDataGrid.SelectedIndex = 0;
            }
            else
            {
                OrderItemsDataGrid.ItemsSource = null;
                SelectedOrderHeaderTextBlock.Text = "Заказы отсутствуют.";
                SelectedOrderSumTextBlock.Text = "0,00 руб.";
            }
        }

        private void ApplyOrdersSearch()
        {
            _orders.Clear();

            string search = SearchOrderBox.Text.Trim();

            foreach (var order in _allOrders)
            {
                if (!string.IsNullOrEmpty(search))
                {
                    bool numberMatches = order.OrderNumber.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
                    bool clientMatches = order.ClientFullName.Contains(search, StringComparison.OrdinalIgnoreCase);

                    if (!numberMatches && !clientMatches)
                    {
                        continue;
                    }
                }

                _orders.Add(order);
            }

            OrdersDataGrid.ItemsSource = null;
            OrdersDataGrid.ItemsSource = _orders;
        }

        private void SearchOrderBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyOrdersSearch();
        }

        private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is OrderRowView selectedOrder)
            {
                _selectedOrderView = selectedOrder;
                SelectedOrderHeaderTextBlock.Text = "Состав заказа №" + selectedOrder.OrderNumber + " (Клиент: " + selectedOrder.ClientFullName + "):";
                SelectedOrderSumTextBlock.Text = selectedOrder.TotalSum.ToString("N2") + " руб.";

                OrderDatePicker.SelectedDate = selectedOrder.OrderDate.ToDateTime(TimeOnly.MinValue);

                LoadOrderItems(selectedOrder.OrderNumber);
            }
        }

        private void LoadOrderItems(int orderNumber)
        {
            DemoDbContext db = new DemoDbContext();

            List<OrderItem> items = db.OrderItems.Include(oi => oi.StockItem).ThenInclude(si => si.Product).ThenInclude(p => p.Manufacturer).Include(oi => oi.StockItem).ThenInclude(si => si.Size).Where(oi => oi.OrderNumber == orderNumber).ToList();

            List<OrderItemRowView> itemViewList = new List<OrderItemRowView>();
            decimal total = 0;

            foreach (var oi in items)
            {
                decimal rowTotal = oi.Quantity * oi.Price;
                total += rowTotal;

                OrderItemRowView row = new OrderItemRowView
                {
                    OrderItemId = oi.OrderItemId,
                    OrderNumber = oi.OrderNumber,
                    ProductId = oi.ProductId,
                    SizeId = oi.SizeId,
                    ProductTitle = oi.StockItem?.Product != null ? (oi.StockItem.Product.Manufacturer?.Manufacturer1 ?? string.Empty) + " | " + oi.StockItem.Product.ProductName : "Товарная позиция",
                    SizeDisplay = oi.StockItem?.Size != null ? oi.StockItem.Size.SizeName.ToString("0.#") : "Стандарт",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.Price,
                    TotalPrice = rowTotal,
                    UnitPriceDisplay = oi.Price.ToString("N2") + " руб.",
                    TotalPriceDisplay = rowTotal.ToString("N2") + " руб."
                };

                itemViewList.Add(row);
            }

            OrderItemsDataGrid.ItemsSource = null;
            OrderItemsDataGrid.ItemsSource = itemViewList;
            SelectedOrderSumTextBlock.Text = total.ToString("N2") + " руб.";
        }

        private void SaveDateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrderView == null)
            {
                MessageBox.Show("Пожалуйста, выберите заказ для изменения даты.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!OrderDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите корректную дату заказа.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DemoDbContext db = new DemoDbContext();
                var order = db.Orders.FirstOrDefault(o => o.OrderNumber == _selectedOrderView.OrderNumber);

                if (order != null)
                {
                    order.OrderDate = DateOnly.FromDateTime(OrderDatePicker.SelectedDate.Value);
                    db.SaveChanges();

                    MessageBox.Show("Дата заказа №" + order.OrderNumber + " успешно изменена на " + order.OrderDate.ToString("dd.MM.yyyy") + "!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOrders();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении даты заказа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteOrderItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is OrderItemRowView row)
            {
                var confirm = MessageBox.Show("Вы действительно хотите удалить позицию \"" + row.ProductTitle + "\" из заказа №" + row.OrderNumber + "?\nТовар вернется на склад.", "Предупреждение об удалении", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                try
                {
                    DemoDbContext db = new DemoDbContext();

                    var item = db.OrderItems.FirstOrDefault(oi => oi.OrderItemId == row.OrderItemId);
                    if (item != null)
                    {
                        var stock = db.StockItems.FirstOrDefault(s => s.ProductId == item.ProductId && s.SizeId == item.SizeId);
                        if (stock != null)
                        {
                            stock.QuantityForOrders += item.Quantity;
                        }

                        db.OrderItems.Remove(item);

                        bool hasOtherItems = db.OrderItems.Any(oi => oi.OrderNumber == row.OrderNumber && oi.OrderItemId != row.OrderItemId);
                        if (!hasOtherItems)
                        {
                            var order = db.Orders.FirstOrDefault(o => o.OrderNumber == row.OrderNumber);
                            if (order != null)
                            {
                                db.Orders.Remove(order);
                            }
                        }

                        db.SaveChanges();

                        MessageBox.Show("Позиция успешно удалена из заказа, остаток на складе возвращен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadOrders();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении позиции заказа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrderView == null)
            {
                MessageBox.Show("Пожалуйста, выберите заказ для удаления.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show("Вы действительно хотите удалить заказ №" + _selectedOrderView.OrderNumber + "? Это действие необратимо, товары будут возвращены на склад.", "Предупреждение о безвозвратном удалении", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                DemoDbContext db = new DemoDbContext();

                var order = db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderNumber == _selectedOrderView.OrderNumber);

                if (order != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var stock = db.StockItems.FirstOrDefault(s => s.ProductId == item.ProductId && s.SizeId == item.SizeId);
                        if (stock != null)
                        {
                            stock.QuantityForOrders += item.Quantity;
                        }
                    }

                    db.OrderItems.RemoveRange(order.OrderItems);
                    db.Orders.Remove(order);
                    db.SaveChanges();

                    MessageBox.Show("Заказ №" + _selectedOrderView.OrderNumber + " успешно удален.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOrders();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении заказа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNewOrderButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Для добавления нового заказа выберите товары в каталоге, укажите нужный размер и количество, после чего откройте корзину и подтвердите заказ.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class OrderRowView
    {
        public int OrderNumber { get; set; }
        public DateOnly OrderDate { get; set; }
        public string OrderDateDisplay { get; set; }
        public string ClientFullName { get; set; }
        public decimal TotalSum { get; set; }
        public string TotalSumDisplay { get; set; }
        public Order OrderEntity { get; set; }
    }

    public class OrderItemRowView
    {
        public int OrderItemId { get; set; }
        public int OrderNumber { get; set; }
        public int ProductId { get; set; }
        public int SizeId { get; set; }
        public string ProductTitle { get; set; }
        public string SizeDisplay { get; set; }
        public short Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string UnitPriceDisplay { get; set; }
        public string TotalPriceDisplay { get; set; }
    }
}
