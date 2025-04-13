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
    public class PullRequestManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _orgRepoOwner;
        private readonly string _orgRepoName;
        private readonly string _userRepoOwner;

        public PullRequestManager(HttpClient httpClient, string orgRepoOwner, string orgRepoName, string userRepoOwner)
        {
            _httpClient = httpClient;
            _orgRepoOwner = orgRepoOwner;
            _orgRepoName = orgRepoName;
            _userRepoOwner = userRepoOwner;
        }

        public  async Task<bool> PackagedPullRequest(string branchName, string title)
        {
            Debug.WriteLine("Entering PackagedPullRequest...");
            try
            {
                var url = $"https://api.github.com/repos/{_orgRepoOwner}/{_orgRepoName}/pulls";
                Debug.WriteLine($"Creating pull request from branch '{branchName}' with URL: {url}");

                var data = new
                {
                    title = title,
                    head = $"{_userRepoOwner}:{branchName}",
                    @base = "main",
                    body = "A new post for review."
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error creating pull request: {errorMessage}");
                    Console.WriteLine($"Error creating Pull Request: {errorMessage}");
                    return false;
                }

                Debug.WriteLine("Pull request created successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PackagedPullRequest: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting PackagedPullRequest...");
            }
        }

    }
}
