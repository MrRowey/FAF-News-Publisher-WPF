using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WpfPublisher.Core
{
    public class ForkManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _orgRepoOwner;
        private readonly string _orgRepoName;
        private readonly string _userRepoOwner;
        private readonly string _defaultBranch;

        public ForkManager(HttpClient httpClient, string orgRepoOwner, string orgRepoName, string userRepoOwner, string defaultBranch)
        {
            _httpClient = httpClient;
            _orgRepoOwner = orgRepoOwner;
            _orgRepoName = orgRepoName;
            _userRepoOwner = userRepoOwner;
            _defaultBranch = defaultBranch;
        }
        public async Task<(string forkName, bool isNewFork)> EnsureForkExists()
        {
            Debug.WriteLine("Entering EnsureForkExists...");
            try
            {
                // Step 1: Check if the repository is already forked with the same name as the organization repository
                Debug.WriteLine("Checking if the repository is already forked...");
                var forkUrl = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}";
                var response = await _httpClient.GetAsync(forkUrl);

                if (response.IsSuccessStatusCode)
                {
                    var repoInfo = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    if (repoInfo != null && repoInfo.fork == true && repoInfo.parent.full_name == $"{_orgRepoOwner}/{_orgRepoName}")
                    {
                        Debug.WriteLine($"User already has a fork: {repoInfo.full_name}");
                        return (repoInfo.full_name, false); // Return the name of the existing fork
                    }
                    else
                    {
                        Debug.WriteLine("Repository exists but is not a fork of the organization repository.");
                        throw new Exception("Repository exists but is not a fork of the organization repository.");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Debug.WriteLine("Repository not found. Proceeding to create a new fork...");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error checking fork status: {response.StatusCode} - {response.ReasonPhrase}");
                    throw new Exception($"Error checking fork status: {response.StatusCode} - {response.ReasonPhrase}");
                }

                // Step 2: If no fork exists, create a new fork
                Debug.WriteLine("No existing fork found. Creating a new fork...");
                var createForkUrl = $"https://api.github.com/repos/{_orgRepoOwner}/{_orgRepoName}/forks";
                response = await _httpClient.PostAsync(createForkUrl, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error creating fork: {errorMessage}");
                    throw new Exception($"Error creating fork: {errorMessage}");
                }

                Debug.WriteLine("Fork created successfully. Waiting for fork to be ready...");
                await Task.Delay(5000); // Wait for the fork to be ready

                // Step 3: Fetch the fork's name
                var forkInfo = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                Debug.WriteLine($"Fork created: {forkInfo.full_name}");
                return (forkInfo.full_name, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in EnsureForkExists: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting EnsureForkExists...");
            }
        }

        public async Task UpdateForkMainBranch()
        {
            Debug.WriteLine("Entering UpdateForkMainBranch...");
            try
            {
                var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/merges";
                Debug.WriteLine($"Updating fork's main branch with URL: {url}");

                var data = new
                {
                    @base = _defaultBranch, // The Fork's main branch
                    head = $"{_orgRepoOwner}:{_defaultBranch}", // The Organization's main branch
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Debug.WriteLine("No Updates avalable for the fork's main branch. continuing...");
                        return;
                    }

                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error updating fork's main branch: {errorMessage}");
                    throw new Exception($"Error updating the fork's main branch: {errorMessage}");
                }

                Debug.WriteLine("Fork's main branch updated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateForkMainBranch: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting UpdateForkMainBranch...");
            }
        }

    }
}
