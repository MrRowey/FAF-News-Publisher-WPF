using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using WpfPublisher.Auth;

namespace WpfPublisher.Auth
{
    public class GitHubOAuth
    {
        private readonly OAuthConfig _config;
        private readonly TokenManager _tokenManager;
        private readonly AuthorizationManager _authManager;

        public GitHubOAuth()
        {
            _config = new OAuthConfig("q", "q"); // Assing ClientID & Client Secret
            _tokenManager = new TokenManager(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            _authManager = new AuthorizationManager(_config);
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
                   new KeyValuePair<string, string>("client_id", _config.ClientId),
            new KeyValuePair<string, string>("client_secret", _config.ClientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _config.RedirectUri)
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
                var accessToken = TokenManager.ParseAccessToken(responseString);
                _tokenManager.SaveAccessToken(accessToken);


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

        public string LoadAccessToken() => _tokenManager.LoadAccessToken();

        public async Task<string> CaptureAuthCodeAsync() => await _authManager.CaptureAuthCodeAsync();
    }
}
