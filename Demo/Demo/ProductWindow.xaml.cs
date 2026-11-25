using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using ChudoObuv.Models;

namespace ChudoObuv
{
    public partial class ProductWindow : Window
    {
        private readonly int _productId;
        public User? CurrentUser { get; set; }

        private Product? _product;
        private decimal _finalPrice;

        public ProductWindow(int productId, User? currentUser)
        {
            InitializeComponent();
            _productId = productId;
            CurrentUser = currentUser;

            SetupUserInfo();
            LoadProductDetails();
            UpdateCartButton();
        }

        private void SetupUserInfo()
        {
            if (CurrentUser == null)
            {
                UserLogin.Text = "Гость";
                OrderControlsPanel.Visibility = Visibility.Collapsed;
                GoToCartButton.Visibility = Visibility.Collapsed;
                GuestNoticeTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                string roleName = CurrentUser.Role?.RoleName ?? "Пользователь";
                UserLogin.Text = CurrentUser.LastName + " " + CurrentUser.Name + " " + CurrentUser.Surname + " (" + roleName + ")";
                OrderControlsPanel.Visibility = Visibility.Visible;
                GoToCartButton.Visibility = Visibility.Visible;
                GuestNoticeTextBlock.Visibility = Visibility.Collapsed;
            }
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

        private void LoadProductDetails()
        {
            DemoDbContext db = new DemoDbContext();

            _product = db.Products.Include(p => p.Manufacturer).Include(p => p.Category).Include(p => p.StockItems).ThenInclude(si => si.Size).FirstOrDefault(p => p.ProductId == _productId);

            if (_product == null)
            {
                MessageBox.Show("Запрашиваемый товар не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            TitleTextBlock.Text = _product.ProductName;
            ManufacturerTextBlock.Text = "Производство: " + (_product.Manufacturer?.Manufacturer1 ?? "Неизвестно");
            CategoryTextBlock.Text = "Категория: " + (_product.Category?.CategoryName ?? "Не указана");
            CompoundTextBlock.Text = "Состав: " + (_product.Compound ?? "Не указан");
            DescriptionTextBlock.Text = "Описание: " + (_product.Description ?? "Описание отсутствует");

            var (prevYear, prevMonth) = GetPreviousCalculationMonth(db);
            bool hasOrdersInPrevMonth = db.OrderItems.Any(oi => oi.ProductId == _productId && oi.OrderNumberNavigation.OrderDate.Year == prevYear && oi.OrderNumberNavigation.OrderDate.Month == prevMonth);

            bool hasDiscount = !hasOrdersInPrevMonth;
            _finalPrice = hasDiscount ? Math.Round(_product.Price * 0.75m, 2) : _product.Price;

            PriceTextBlock.Text = _finalPrice.ToString("N2") + " руб.";
            if (hasDiscount)
            {
                OldPriceTextBlock.Text = _product.Price.ToString("N2") + " руб.";
                DiscountBadgeTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                OldPriceTextBlock.Text = string.Empty;
                DiscountBadgeTextBlock.Visibility = Visibility.Collapsed;
            }

            ProductImage.Source = LoadPicture(_product.Picture);

            List<string> allSizes = new List<string>();
            foreach (var s in _product.StockItems.OrderBy(s => s.Size.SizeName))
            {
                allSizes.Add(s.Size.SizeName.ToString("0.#"));
            }
            SizeRangeTextBlock.Text = "Размерный ряд: " + (allSizes.Count > 0 ? string.Join(", ", allSizes) : "отсутствует");

            List<StockItem> inStockItems = _product.StockItems.Where(s => s.QuantityForOrders > 0).OrderBy(s => s.Size.SizeName).ToList();
            List<string> availableTextList = new List<string>();

            SizeComboBox.Items.Clear();
            foreach (var stock in inStockItems)
            {
                availableTextList.Add(stock.Size.SizeName.ToString("0.#") + " (" + stock.QuantityForOrders + " шт.)");
                SizeComboBox.Items.Add(new SizeChoiceItem
                {
                    SizeId = stock.SizeId,
                    SizeName = stock.Size.SizeName,
                    AvailableStock = stock.QuantityForOrders
                });
            }

            AvailableSizesTextBlock.Text = "Доступные для заказа размеры: " + (availableTextList.Count > 0 ? string.Join(", ", availableTextList) : "нет в наличии");

            if (SizeComboBox.Items.Count > 0)
            {
                SizeComboBox.SelectedIndex = 0;
            }
            else
            {
                AddToCartButton.IsEnabled = false;
            }
        }

        public static BitmapImage LoadPicture(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "picture.png";
            }

            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri("pack://application:,,,/Images/" + fileName, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch
            {
                BitmapImage fallback = new BitmapImage();
                fallback.BeginInit();
                fallback.UriSource = new Uri("pack://application:,,,/Images/picture.png", UriKind.Absolute);
                fallback.CacheOption = BitmapCacheOption.OnLoad;
                fallback.EndInit();
                return fallback;
            }
        }

        private void UpdateCartButton()
        {
            GoToCartButton.Content = "Оформить заказ (" + OrderCart.TotalCount + ")";
        }

        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_product == null)
            {
                return;
            }

            if (SizeComboBox.SelectedItem is not SizeChoiceItem selectedSize)
            {
                MessageBox.Show("Пожалуйста, выберите размер обуви.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityBox.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Количество должно быть целым положительным числом больше нуля.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var existingInCart = OrderCart.Items.FirstOrDefault(i => i.ProductId == _product.ProductId && i.SizeId == selectedSize.SizeId);
            int currentInCart = existingInCart?.Quantity ?? 0;

            if (currentInCart + qty > selectedSize.AvailableStock)
            {
                MessageBox.Show("Недостаточно товара на складе!\nВ наличии: " + selectedSize.AvailableStock + " шт.\nУже в заказе: " + currentInCart + " шт.\nДоступно для добавления: " + (selectedSize.AvailableStock - currentInCart) + " шт.", "Ошибка наличия", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (existingInCart != null)
            {
                existingInCart.Quantity = (short)(existingInCart.Quantity + qty);
            }
            else
            {
                OrderCart.Items.Add(new CartItem
                {
                    ProductId = _product.ProductId,
                    Title = _product.ProductName,
                    Manufacturer = _product.Manufacturer?.Manufacturer1 ?? string.Empty,
                    SizeId = selectedSize.SizeId,
                    SizeName = selectedSize.SizeName,
                    Quantity = (short)qty,
                    UnitPrice = _finalPrice,
                    AvailableStock = selectedSize.AvailableStock
                });
            }

            MessageBox.Show("Товар \"" + _product.ProductName + "\" (размер " + selectedSize.SizeName.ToString("0.#") + ", " + qty + " шт.) добавлен в заказ!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateCartButton();
        }

        private void GoToCartButton_Click(object sender, RoutedEventArgs e)
        {
            CartWindow cartWindow = new CartWindow(CurrentUser);
            cartWindow.Owner = this;
            cartWindow.ShowDialog();

            LoadProductDetails();
            UpdateCartButton();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class SizeChoiceItem
    {
        public int SizeId { get; set; }
        public decimal SizeName { get; set; }
        public int AvailableStock { get; set; }

        public override string ToString()
        {
            return "Размер " + SizeName.ToString("0.#") + " (в наличии: " + AvailableStock + " шт.)";
        }
    }
}
