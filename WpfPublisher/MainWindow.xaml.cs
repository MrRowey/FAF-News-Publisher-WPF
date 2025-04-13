using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfPublisher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Load Nav Items
            SideNav.ItemsSource = new List<NavItem>
            {
                new NavItem { Name = "Home", Icon = "🏠", Tag = "HomePage" },
                new NavItem { Name = "Posts", Icon = "📝", Tag = "PostPage" },
                new NavItem { Name = "Settings", Icon = "⚙️", Tag = "SettingsPage" },
            };

            // Load the default page (e.g., LoginPage) on startup
            MainFrame.Navigate(new Uri("LoginPage.xaml", UriKind.Relative));
        }

        private void SideNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SideNav.SelectedItem is NavItem selectedItem)
            {
                // Navigate to the selected page
                MainFrame.Navigate(new Uri(selectedItem.Page, UriKind.Relative));
            }
        }
    }

    public class NavItem
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Page { get; set; }
        public string Tag { get; set; }
    }
}