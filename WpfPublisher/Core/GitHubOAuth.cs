using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;

namespace WpfPublisher.Core
{
    public class GitHubOAuth
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri = "http://localhost/oauth-callback";

        public GitHubOAuth()
        {
            // Constructor can be used for any initialization if needed
            _clientId = "Ov23lirrCNvEXZDOD5P7"; // Environment.GetEnvironmentVariable("CLIENT_ID");
            _clientSecret = "740dcd6626ed8ff26cfa38b75f3b012cfb3ec54c"; // Environment.GetEnvironmentVariable("CLIENT_SECRET");

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
            var client = new HttpClient();
            var url = "https://github.com/login/oauth/access_token";

            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _redirectUri)
            ]);

            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            // Extract the access token from the response (this assumes the response is in the form of "access_token=xxxx&scope=repo&token_type=bearer")
            var accessToken = ParseAccessToken(responseString);
            return accessToken;
        }

        private static string ParseAccessToken(string response)
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(response);
            var accessToken = queryParams["access_token"] ?? throw new InvalidOperationException("Access token not found in the response.");
            return accessToken;
        }

    }
}
