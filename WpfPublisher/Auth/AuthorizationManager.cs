using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using System.Web;

namespace WpfPublisher.Auth
{
    public class AuthorizationManager
    {
        private readonly OAuthConfig _config;

        public AuthorizationManager(OAuthConfig config)
        {
            _config = config;
        }

        public string GetAuthorizationUrl()
        {
            // Construct the authorization URL
            return $"https://github.com/login/oauth/authorize?client_id={_config.ClientId}&redirect_uri={_config.RedirectUri}&scope=repo";
        }

        public async Task<string> CaptureAuthCodeAsync()
        {
            Debug.WriteLine("Starting GitHub OAuth authorization process.");

            var tcs = new TaskCompletionSource<string>();
            var browserWindow = new Window
            {
                Title = "GitHub Authorization",
                Width = 800,
                Height = 600,
            };

            var webView = new WebView2();
            bool isComplete = false;

            webView.NavigationCompleted += (sender, e) =>
            {
                try
                {
                    var uri = new Uri(webView.Source.ToString());
                    Debug.WriteLine($"Navigated to: {uri.AbsoluteUri}");

                    if (uri.AbsoluteUri.StartsWith(_config.RedirectUri, StringComparison.OrdinalIgnoreCase) && !isComplete)
                    {
                        Debug.WriteLine("Redirect URI detected. Parsing query parameters...");

                        var queryParams = HttpUtility.ParseQueryString(uri.Query);
                        var code = queryParams["code"];
                        if (!string.IsNullOrEmpty(code))
                        {
                            Debug.WriteLine("Authorization code successfully retrieved.");
                            isComplete = true;
                            tcs.SetResult(code);
                            browserWindow.Close();
                        }
                        else
                        {
                            Debug.WriteLine("Authorization code not found in the response.");
                            isComplete = true;
                            tcs.SetException(new InvalidOperationException("Authorization code not found in the response."));
                            browserWindow.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!isComplete)
                    {
                        Debug.WriteLine($"Error during navigation: {ex.Message}");
                        isComplete = true;
                        tcs.SetException(ex);
                        browserWindow.Close();
                    }
                }
            };

            browserWindow.Content = webView;
            browserWindow.Show();
            Debug.WriteLine("Browser window displayed for user authorization.");

            var authorizationUrl = GetAuthorizationUrl();
            Debug.WriteLine($"Navigating to GitHub authorization URL: {authorizationUrl}");
            await webView.EnsureCoreWebView2Async();
            webView.Source = new Uri(authorizationUrl);

            return await tcs.Task;
        }
    }
}
