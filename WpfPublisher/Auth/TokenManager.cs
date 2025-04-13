using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WpfPublisher.Auth
{
    public class TokenManager
    {
        private readonly string _tokenFilePath;

        public TokenManager(string appDataFolder)
        {
            _tokenFilePath = Path.Combine(appDataFolder, "access_token.txt");
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

        public static string ParseAccessToken(string response)
        {
            var queryParams = HttpUtility.ParseQueryString(response);
            return queryParams["access_token"] ?? throw new InvalidOperationException("Access token not found in teh reponce. ");
        }

    }
}
