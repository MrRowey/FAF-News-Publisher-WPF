using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using System.Windows;
using System.Diagnostics;
using System.IO;
using Microsoft.Web.WebView2.Wpf;

namespace WpfPublisher.Core
{
    public class GitHubOAuth
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri = "http://localhost/oauth-callback";
        private readonly string _tokenFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WpfPublisher", "access_token.txt");

        public GitHubOAuth()
        {
            // Constructor can be used for any initialization if needed
            _clientId = "Ov23lirrCNvEXZDOD5P7"; // Environment.GetEnvironmentVariable("CLIENT_ID");
            _clientSecret = ""; // Environment.GetEnvironmentVariable("CLIENT_SECRET");

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                throw new InvalidOperationException("CLIENT_ID and CLIENT_SECRET environment variables must be set.");
            }
        }

        public string GetAuthorizationUrl()
        {
            // Returns the URL the user needs to visit for GitHub OAuth authorization
            return $"https://github.com/login/oauth/authorize?client_id={_clientId}&redirect_uri={_redirectUri}&scope=repo";
        }

        public async Task<string> GetAccessToken(string code)
        {
            Debug.WriteLine("Entering GetAccsesToken...");
            try
            {
                var client = new HttpClient();
                var url = "https://github.com/login/oauth/access_token";

                var content = new FormUrlEncodedContent(
                [
                   new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _redirectUri)
                ]);

                Debug.WriteLine("Sending request to GitHub for access token...");
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Raw Response from GitHub: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    throw new InvalidOperationException($"GitHub OAuth error: {response.StatusCode} - {response.ReasonPhrase}");
                }

                // Extract the access token from the response (this assumes the response is in the form of "access_token=xxxx&scope=repo&token_type=bearer")
                var accessToken = ParseAccessToken(responseString);

                // Save the access token to a file for later use
                SaveAccessToken(accessToken);

                Debug.WriteLine("Access token retrieved and saved successfully.");
                return accessToken;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAccessToken: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting GetAccsesToken...");
            }
        }

        private static string ParseAccessToken(string response)
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(response);
            var accessToken = queryParams["access_token"] ?? throw new InvalidOperationException("Access token not found in the response.");
            return accessToken;
        }

        public string LoadAccessToken()
        {
            if (File.Exists(_tokenFilePath))
            {
                return File.ReadAllText(_tokenFilePath);
            }

            return null;
        }

        public void SaveAccessToken(string accessToken)
        {
            var directory = Path.GetDirectoryName(_tokenFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(_tokenFilePath, accessToken);
        }

        public async Task<string> CaptureAuthCodeAsync()
        {
            Debug.WriteLine("Starting GitHub OAuth authorization process.");

            var tcs = new TaskCompletionSource<string>();
            var browserWindow = new Window
            {
                Title = "Github Authorization",
                Width = 800,
                Height = 600,
            };

            var webView = new WebView2();
            bool isComplete = false; // Flag to track if the process is completed once

            webView.NavigationCompleted += (sender, e) =>
            {
                try
                {

                    var uri = new Uri(webView.Source.ToString());
                    Debug.WriteLine($"Navigated to: {uri.AbsoluteUri}");

                    if (uri.AbsoluteUri.StartsWith(_redirectUri, StringComparison.OrdinalIgnoreCase) && !isComplete)
                    {
                        Debug.WriteLine("Redirect URI detected. Parsing query parameters...");

                        var queryParams = HttpUtility.ParseQueryString(uri.Query);
                        var code = queryParams["code"];
                        if (!string.IsNullOrEmpty(code))
                        {
                            Debug.WriteLine("Authorization code successfully retrieved.");
                            isComplete = true; // Set the flag to true to prevent multiple completions
                            tcs.SetResult(code);
                            browserWindow.Close();
                        }
                        else
                        {
                            Debug.WriteLine("Authorization code not found in the response.");
                            isComplete = true; // Set the flag to true to prevent multiple completions
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
                        isComplete = true; // Set the flag to true to prevent multiple completions
                        tcs.SetException(ex);
                        browserWindow.Close();
                    }
                }
            };


            browserWindow.Content = webView;
            browserWindow.Show();
            Debug.WriteLine("Browser window displayed for user authorization.");

            // Navigate to the GitHub authorization URL
            var authorizationUrl = GetAuthorizationUrl();
            Debug.WriteLine($"Navigating to GitHub authorization URL: {authorizationUrl}");
            await webView.EnsureCoreWebView2Async();
            webView.Source = new Uri(authorizationUrl);

            return await tcs.Task;
        }

    }
}
