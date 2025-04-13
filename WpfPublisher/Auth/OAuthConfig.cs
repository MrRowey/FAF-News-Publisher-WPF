using System;

namespace WpfPublisher.Auth
{
    public class OAuthConfig
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string RedirectUri { get; } = "http://localhost/oauth-callback";

        public OAuthConfig(string clientId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("CLIENT_ID and CLIENT_SECRET environment variables must be set.");
            }

            ClientId = clientId;
            ClientSecret = clientSecret;
        }
    }
}
