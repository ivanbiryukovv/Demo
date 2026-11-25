using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using ChudoObuv.Models;

namespace ChudoObuv
{
    public partial class MainWindow : Window
    {
        public User? CurrentUser { get; set; }

        List<ProductView> _products = new List<ProductView>();
        List<ProductView> _allProducts = new List<ProductView>();
        private bool _isLoaded = false;

        public MainWindow(User? user)
        {
            InitializeComponent();
            CurrentUser = user;
            SetupUserInterface();
            LoadCategories();
            LoadProducts();
            UpdateCartButton();
            _isLoaded = true;
        }

        private void SetupUserInterface()
        {
            if (CurrentUser == null)
            {
                UserInfoTextBlock.Text = "Гость (Неавторизованный)";
                FilterPanel.Visibility = Visibility.Collapsed;
                CartButton.Visibility = Visibility.Collapsed;
                OrdersButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                string roleName = CurrentUser.Role?.RoleName ?? "Пользователь";
                UserInfoTextBlock.Text = CurrentUser.LastName + " " + CurrentUser.Name + " " + CurrentUser.Surname + " (" + roleName + ")";

                if (CurrentUser.RoleId == 3 || CurrentUser.RoleId == 4)
                {
                    OrdersButton.Content = "Управление заказами";
                    OrdersButton.Visibility = Visibility.Visible;
                }
                else
                {
                    OrdersButton.Content = "Мои заказы";
                    OrdersButton.Visibility = Visibility.Visible;
                }

                CartButton.Visibility = Visibility.Visible;
                FilterPanel.Visibility = Visibility.Visible;
            }
        }

        private void LoadCategories()
        {
            DemoDbContext db = new DemoDbContext();
            List<string> categories = db.ProductCategories.OrderBy(c => c.CategoryName).Select(c => c.CategoryName).ToList();

            CategoryComboBox.Items.Clear();
            CategoryComboBox.Items.Add("Все категории");
            foreach (var cat in categories)
            {
                CategoryComboBox.Items.Add(cat);
            }
            CategoryComboBox.SelectedIndex = 0;
        }

        private (int year, int month) GetPreviousCalculationMonth(DemoDbContext db)
        {
            DateTime now = DateTime.Today;
            int month = now.Month == 1 ? 12 : now.Month - 1;
            int year = now.Month == 1 ? now.Year - 1 : now.Year;

            if (!db.Orders.Any(o => o.OrderDate.Year == year && o.OrderDate.Month == month) && db.Orders.Any())
            {
                var maxOrderDate = db.Orders.Max(o => o.OrderDate);
                month = maxOrderDate.Month == 1 ? 12 : maxOrderDate.Month - 1;
                year = maxOrderDate.Month == 1 ? maxOrderDate.Year - 1 : maxOrderDate.Year;
            }

            return (year, month);
        }

        public void LoadProducts()
        {
            _allProducts.Clear();

            DemoDbContext db = new DemoDbContext();

            List<Product> products = db.Products.Include(p => p.Manufacturer).Include(p => p.Category).Include(p => p.StockItems).ToList();

            var (prevYear, prevMonth) = GetPreviousCalculationMonth(db);
            HashSet<int> orderedProductIds = db.OrderItems.Where(oi => oi.OrderNumberNavigation.OrderDate.Year == prevYear && oi.OrderNumberNavigation.OrderDate.Month == prevMonth).Select(oi => oi.ProductId).Distinct().ToHashSet();

            Brush lowStockBrush = (Brush)new BrushConverter().ConvertFrom("#ff8080")!;

            foreach (var product in products)
            {
                int totalQuantity = product.StockItems.Sum(s => s.QuantityForOrders);
                bool hasDiscount = !orderedProductIds.Contains(product.ProductId);
                decimal finalPrice = hasDiscount ? Math.Round(product.Price * 0.75m, 2) : product.Price;

                ProductView view = new ProductView
                {
                    ProductId = product.ProductId,
                    ProductionAndName = (product.Manufacturer?.Manufacturer1 ?? "Производитель") + " | " + product.ProductName,
                    CategoryName = product.Category?.CategoryName ?? "Без категории",
                    Description = product.Description ?? string.Empty,
                    Compound = product.Compound ?? string.Empty,
                    TotalQuantity = totalQuantity,
                    QuantityStatus = totalQuantity > 5 ? "много (" + totalQuantity + " шт.)" : "мало (" + totalQuantity + " шт.)",
                    OriginalPrice = product.Price,
                    FinalPrice = finalPrice,
                    HasDiscount = hasDiscount,
                    DisplayPrice = finalPrice.ToString("N2") + " руб.",
                    OldPriceDisplay = hasDiscount ? product.Price.ToString("N2") + " руб." : string.Empty,
                    DiscountVisibility = hasDiscount ? Visibility.Visible : Visibility.Collapsed,
                    Background = totalQuantity <= 3 ? lowStockBrush : Brushes.White,
                    Picture = ProductWindow.LoadPicture(product.Picture)
                };

                _allProducts.Add(view);
            }

            ApplyFilterAndSort();
        }

        private void ApplyFilterAndSort()
        {
            _products.Clear();

            string searchText = SearchBox?.Text?.Trim() ?? string.Empty;
            string selectedCategory = CategoryComboBox?.SelectedIndex > 0 ? CategoryComboBox.SelectedItem.ToString()! : string.Empty;

            foreach (var product in _allProducts)
            {
                if (!string.IsNullOrEmpty(selectedCategory) && product.CategoryName != selectedCategory)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(searchText))
                {
                    bool nameMatches = product.ProductionAndName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                    bool descMatches = product.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase);

                    if (!nameMatches && !descMatches)
                    {
                        continue;
                    }
                }

                _products.Add(product);
            }

            if (SortComboBox != null)
            {
                if (SortComboBox.SelectedIndex == 1)
                {
                    _products = _products.OrderBy(p => p.FinalPrice).ToList();
                }
                else if (SortComboBox.SelectedIndex == 2)
                {
                    _products = _products.OrderByDescending(p => p.FinalPrice).ToList();
                }
            }

            ProductsList.ItemsSource = null;
            ProductsList.ItemsSource = _products;

            if (ItemsCountTextBlock != null)
            {
                ItemsCountTextBlock.Text = "Показано: " + _products.Count + " из " + _allProducts.Count;
            }
        }

        public void UpdateCartButton()
        {
            CartButton.Content = "Корзина (" + OrderCart.TotalCount + ")";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoaded)
            {
                ApplyFilterAndSort();
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded)
            {
                ApplyFilterAndSort();
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded)
            {
                ApplyFilterAndSort();
            }
        }

        private void ProductBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ProductView view)
            {
                ProductWindow productWindow = new ProductWindow(view.ProductId, CurrentUser);
                productWindow.Owner = this;
                productWindow.ShowDialog();

                LoadProducts();
                UpdateCartButton();
            }
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            CartWindow cartWindow = new CartWindow(CurrentUser);
            cartWindow.Owner = this;
            cartWindow.ShowDialog();

            LoadProducts();
            UpdateCartButton();
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            OrdersWindow ordersWindow = new OrdersWindow(CurrentUser);
            ordersWindow.Owner = this;
            ordersWindow.ShowDialog();

            LoadProducts();
            UpdateCartButton();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }

    public class ProductView
    {
        public int ProductId { get; set; }
        public BitmapImage Picture { get; set; }
        public string ProductionAndName { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public string Compound { get; set; }
        public int TotalQuantity { get; set; }
        public string QuantityStatus { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public bool HasDiscount { get; set; }
        public string DisplayPrice { get; set; }
        public string OldPriceDisplay { get; set; }
        public Visibility DiscountVisibility { get; set; }
        public Brush Background { get; set; }
    }
}
