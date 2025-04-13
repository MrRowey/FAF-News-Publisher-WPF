using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;

namespace WpfPublisher.Core
{
    public class GitHubPullRequest
    {
        private readonly string _accessToken;
        private static readonly HttpClient _httpClient = new();

        private readonly string _orgRepoOwner = "FAForever";
        private readonly string _orgRepoName = "website-news";
        private readonly string _defaultBranch = "main"; // Usually "main"
        private readonly string _userRepoOwner; // Your GitHub Username

        public GitHubPullRequest(string accessToken, string userRepoOwner)
        {
            Debug.WriteLine("Initializing GitHubPullRequest...");
            _accessToken = accessToken;
            _userRepoOwner = userRepoOwner;

            Debug.WriteLine($"Access Token: {_accessToken}");

            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", "token " + _accessToken);
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "JekyllPublisher");
            }
        }

        public async Task CreatePullRequest(string title, string content)
        {
            Debug.WriteLine("Entering CreatePullRequest...");
            try
            {
                // Step 1: Ensure that the Repository is forked
                Debug.WriteLine("Checking if repository is forked...");
                var (forkName, isNewFork) = await EnsureForkExists();
                Debug.WriteLine($"Forked repository name: {forkName}, Is New Fork: {isNewFork}");

                // Step 2: Ensure the fork's main branch is up-to-date with the organization's main branch
                if (!isNewFork)
                {
                Debug.WriteLine("Updating the fork's main branch...");
                await UpdateForkMainBranch();
                Debug.WriteLine("Fork's main branch updated successfully.");
                }
                else
                {
                    Debug.WriteLine("Fork is new, no need to update the main branch.");
                }

                // Step 3: Get the latest commit SHA from the fork's main branch
                Debug.WriteLine("Getting the latest commit SHA...");
                var commitSha = await GetLatestCommitSha();

                // Step 4: Create a new branch from the latest commit SHA
                Debug.WriteLine($"Creating a new branch: post/{title.Replace(" ", "-")}...");
                var newBranchName = $"post/{title.Replace(" ", "-")}";
                await CreateNewBranch(commitSha, newBranchName);

                // Step 5: Create the markdown file in the '_posts' folder
                Debug.WriteLine($"Creating markdown file for post: {title}...");
                var fileName = $"{DateTime.Now:yyyy-MM-dd}-{title.Replace(" ", "-")}.md";
                var fileContent = $"---\ntitle: {title}\ndate: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nlayout: post\n---\n\n{content}";
                await CreateFile(fileName, fileContent, newBranchName);

                // Step 6: Create a Pull Request from the forked repository's new branch to the organization's main branch
                Debug.WriteLine("Creating a pull request...");
                var prCreated = await PackagedPullRequest(newBranchName, title);

                if (prCreated)
                {
                    Debug.WriteLine("Pull request created successfully.");
                    Console.WriteLine("Your post has been submitted for review!");
                }
                else
                {
                    Debug.WriteLine("Error creating pull request.");
                    Console.WriteLine("Error creating Pull Request.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreatePullRequest: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting CreatePullRequest...");
            }
        }

        private async Task<bool> IsForkedRepository()
        {
            Debug.WriteLine("Entering IsForkedRepository...");
            try
            {
                var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}";
                Debug.WriteLine($"Checking fork status with URL: {url}");
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var repoInfo = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    Debug.WriteLine("Repository fork status retrieved successfully.");
                    return repoInfo.fork && repoInfo.parent.full_name == $"{_orgRepoOwner}/{_orgRepoName}";
                }

                Debug.WriteLine("Repository is not forked.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in IsForkedRepository: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting IsForkedRepository...");
            }
        }

        private async Task ForkRepository()
        {
            Debug.WriteLine("Entering ForkRepository...");
            try
            {
                var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/forks";
                Debug.WriteLine($"Forking repository with URL: {url}");

                var response = await _httpClient.PostAsync(url, null);

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Response from GitHub: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error forking repository: {response.StatusCode} - {response.ReasonPhrase}");
                    throw new Exception($"Error forking the repository: {responseContent}");
                }

                Debug.WriteLine("Repository forked successfully. Waiting for fork to be ready...");
                await Task.Delay(5000); // GitHub may take some time to create the fork
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ForkRepository: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting ForkRepository...");
            }
        }

        private async Task UpdateForkMainBranch()
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
                    if( response.StatusCode == System.Net.HttpStatusCode.NotFound)
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

        private async Task<string> GetLatestCommitSha()
        {
            Debug.WriteLine("Entering GetLatestCommitSha...");
            try
            {
                var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/commits/{_defaultBranch}";
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

        private async Task CreateNewBranch(string commitSha, string branchName)
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

        private async Task CreateFile(string fileName, string content, string branchName)
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

        private async Task<bool> PackagedPullRequest(string branchName, string title)
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
                    @base = _defaultBranch,
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

        private async Task<(string forkName, bool isNewFork)> EnsureForkExists()
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


        private async Task VerifyTokenScopes()
        {
            Debug.WriteLine("Entering VerifyTokenScopes...");
            try
            {
                var url = "https://api.github.com/user";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error verifying token scopes: {errorMessage}");
                    throw new Exception($"Error verifying token scopes: {errorMessage}");
                }

                var scopes = response.Headers.Contains("X-OAuth-Scopes")
                    ? string.Join(", ", response.Headers.GetValues("X-OAuth-Scopes"))
                    : "No Scopes Found";

                Debug.WriteLine($"Token Scopes: {scopes}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VerifyTokenScopes: {ex.Message}");
                throw;
            }
            finally
            {
                Debug.WriteLine("Exiting VerifyTokenScopes...");
            }
        }
    }
}
