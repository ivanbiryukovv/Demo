using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using ChudoObuv.Models;

namespace ChudoObuv
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LoginBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите логин для входа в систему.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DemoDbContext db = new DemoDbContext();

            var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Login == LoginBox.Text.Trim());

            if (user == null)
            {
                MessageBox.Show("Пользователь с таким логином не найден. Проверьте правильность ввода.", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MainWindow mainWindow = new MainWindow(user);
            mainWindow.Show();
            this.Close();
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(null);
            mainWindow.Show();
            this.Close();
        }
    }
}
