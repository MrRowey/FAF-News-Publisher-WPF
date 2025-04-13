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
using System.Windows.Navigation;
using WpfPublisher.Auth;

namespace WpfPublisher
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {

        private readonly GitHubOAuth _auth = new();
        private string _accessToken;

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var authCode = await _auth.CaptureAuthCodeAsync();
                _accessToken = await _auth.GetAccessToken(authCode); // Store the access token

                StatusText.Text = "Login successful!";
                NavigationService.Navigate(new PostPage(_accessToken));
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the login process
                StatusText.Text = "Error: " + ex.Message;
                StatusText.Foreground = Brushes.Red;
            }
        }

        private async void SubmitCode_Click(object sender, RoutedEventArgs e)
        {
            // Get the access token using the provided code
            try
            {
                string code = CodeBox.Text.Trim();
                _accessToken = await _auth.GetAccessToken(code);

                StatusText.Text = "Login successful!";
                NavigationService.Navigate(new PostPage(_accessToken));
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
