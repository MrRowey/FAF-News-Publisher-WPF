using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WpfPublisher.Core
{
    public class FileManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _userRepoOwner;
        private readonly string _orgRepoName;

        public FileManager(HttpClient httpClient, string userRepoOwner, string orgRepoName)
        {
            _httpClient = httpClient;
            _userRepoOwner = userRepoOwner;
            _orgRepoName = orgRepoName;
        }
        public async Task CreateFile(string fileName, string content, string branchName)
        {
            Debug.WriteLine("Entering CreateFile...");
            try
            {
                var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/contents/src/_posts/{fileName}";
                Debug.WriteLine($"Creating file '{fileName}' in branch '{branchName}' with URL: {url}");

                var data = new
                {
                    message = $"Add new post: {fileName}",
                    content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                    branch = branchName
                };

                var json = JsonConvert.SerializeObject(data);
                var contentData = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, contentData);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error creating file: {errorMessage}");
                    throw new Exception($"Error creating the file '{fileName}': {errorMessage}");
                }

                Debug.WriteLine($"File '{fileName}' created successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateFile: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting CreateFile...");
            }
        }



    }
}
