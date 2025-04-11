using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfPublisher.Core;

namespace WpfPublisher
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {

        private readonly GitHubOAuth _auth = new();

        public LoginPage()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            // Open the GitHub OAuth authorization URL in the default web browser
            var url = _auth.GetAuthorizationUrl();
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true});
        }

        private async void SubmitCode_Click(object sender, RoutedEventArgs e)
        {
            // Get the access token using the provided code
            try
            {
                string code = CodeBox.Text.Trim();
                string token = await _auth.GetAccessToken(code);

                MainWindow.AccessToken = token;
                StatusText.Text = "Login successful!";

                NavigationService.Navigate(new PostPage());
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the token retrieval process
                StatusText.Text = "Error: " + ex.Message;
                StatusText.Foreground = Brushes.Red;
            }
        }



    }
}
