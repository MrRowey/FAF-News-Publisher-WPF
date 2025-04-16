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
                new NavItem { Name = "Home", Icon = "🏠", Tag = "Home" },
                new NavItem { Name = "Posts", Icon = "📝", Tag = "PostPage" },
                new NavItem { Name = "Settings", Icon = "⚙️", Tag = "SettingsPage" },
                        new NavItem { Name = "Wiki", Icon = "📖", Tag = "WikiPage" }, // Add Wiki Page
            };

            // Set default selection
            SideNav.SelectedIndex = 0;

            // Load the default page (Home)
            MainFrame.Navigate(new Uri("Home.xaml", UriKind.Relative));
        }

        private void SideNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SideNav.SelectedItem is NavItem selectedItem)
            {
                try
                {
                    // Navigate to the selected page
                    MainFrame.Navigate(new Uri($"{selectedItem.Tag}.xaml", UriKind.Relative));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to navigate to {selectedItem.Tag}: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class NavItem
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Tag { get; set; }
    }
}