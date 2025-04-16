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
    public class BranchManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _userRepoOwner;
        private readonly string _orgRepoName;

        public BranchManager(HttpClient httpClient, string userRepoOwner, string orgRepoName)
        {
            _httpClient = httpClient;
            _userRepoOwner = userRepoOwner;
            _orgRepoName = orgRepoName;
        }
        public async Task<string> GetLatestCommitSha()
        {
            Debug.WriteLine("Entering GetLatestCommitSha...");
            try
            {
                var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/commits/main";
                Debug.WriteLine($"Getting latest commit SHA with URL: {url}");
                var response = await _httpClient.GetStringAsync(url);
                dynamic commit = JsonConvert.DeserializeObject(response);
                Debug.WriteLine($"Latest commit SHA: {commit.sha}");
                return commit.sha;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetLatestCommitSha: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting GetLatestCommitSha...");
            }
        }

        public async Task CreateNewBranch(string commitSha, string branchName)
        {
            Debug.WriteLine("Entering CreateNewBranch...");
            try
            {
                var sanitizedBranchName = branchName.Replace(" ", "-").Replace("_", "-");
                var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/git/refs";
                Debug.WriteLine($"Creating new branch '{sanitizedBranchName}' with URL: {url}");

                var data = new
                {
                    @ref = $"refs/heads/{sanitizedBranchName}",
                    sha = commitSha
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error creating new branch: {errorMessage}");
                    throw new Exception($"Error creating the new branch: {errorMessage}");
                }

                Debug.WriteLine($"Branch '{sanitizedBranchName}' created successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateNewBranch: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting CreateNewBranch...");
            }
        }

    }
}
