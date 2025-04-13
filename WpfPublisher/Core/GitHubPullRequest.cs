using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Runtime.InteropServices;

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

        public GitHubPullRequest(string accessToken, string userRepoOwener)
        {
            _accessToken = accessToken;
            _userRepoOwner = userRepoOwener;

            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", "token " + _accessToken);
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "JekyllPublisher");
            }
        }

        public async Task CreatePullRequest(string title, string content)
        {
            // Step 1: Ensure that the Repository is forked
            var isForked = await IsForkedRepository();
            if (!isForked)
            {
                Console.WriteLine("Forking the Repositoty...");
                await ForkRepository();
                Console.WriteLine("Repository forked successfully.");
            }

            //Step 2: Ensure the fork's main branch is up-to-date with the organisation's main brach
            Console.WriteLine("Updating the fork's main branch...");
            await UpdateForkMainBranch();
            Console.WriteLine("Fork's main branch updated successfully.");

            // Step 3: Get the latest commit SHA from the fork's main branch
            var commitSha = await GetLatestCommitSha();

            // Step 4: Create a new branch from the latest commit SHA
            var newBranchName = $"post/{title.Replace(" ", "-")}";
            await CreateNewBranch(commitSha, newBranchName);

            // Step 5: Create the markdown file in the '_posts' folder
            var fileName = $"{DateTime.Now:yyyy-MM-dd}-{title.Replace(" ", "-")}.md";
            var fileContent = $"---\ntitle: {title}\ndate: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nlayout: post\n---\n\n{content}";
            await CreateFile(fileName, fileContent, newBranchName);

            // Step 4: Create a Pull Request from the forked repository's new branch to the organisation's main branch
            var prCreated = await PackagedPullRequest(newBranchName, title);

            if (prCreated)
            {
                Console.WriteLine("Your post has been submitted for review!");
            }
            else
            {
                Console.WriteLine("Error creating Pull Request.");
            }
        }

        private async Task<bool> IsForkedRepository()
        {
            var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}";
            var response = await _httpClient.GetAsync(url);

            if(!response.IsSuccessStatusCode)
            {
                var repoInfo = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                return repoInfo.fork && repoInfo.parent.full_name == $"{_orgRepoOwner}/{_orgRepoName}";

            }

            return false;
        }

        private async Task ForkRepository()
        {
            var url = $"https://api.github.com/repos/{_orgRepoOwner}/{_orgRepoName}/forks";
            var response = await _httpClient.PostAsync(url, null);
           
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error forking the repository: {errorMessage}");
            }

            // Wait for the fork to be created
            await Task.Delay(5000); // Github may take some time to create the fork
        }

        private async Task UpdateForkMainBranch()
        {
            var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/merges";

            var data = new
            {
                @base = _defaultBranch, // The Fork's main branch
                head = $"{_orgRepoOwner}:{_defaultBranch}", // The Organisation's main branch
            };

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error updating the fork's main branch: {errorMessage}");
            }

        }

        private async Task<string> GetLatestCommitSha()
        {
            var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/commits/{_defaultBranch}";
            var response = await _httpClient.GetStringAsync(url);
            dynamic commit = JsonConvert.DeserializeObject(response);
            return commit.sha;
        }


        private async Task CreateNewBranch(string commitSha, string branchName)
        {
            // Sanitize the branch name to remove special characters
            var sanitizedBranchName = branchName.Replace(" ", "-").Replace("_", "-");

            var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/git/refs";

            // Prepare the data for the new branch
            var data = new
            {
                @ref = $"refs/heads/{sanitizedBranchName}", // Refers to the new branch we want to create
                sha = commitSha // Pointing the new branch to the SHA of the latest commit from the default branch
            };

            // Convert the data to JSON
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating the new branch: {errorMessage}");
            }

            Console.WriteLine($"Branch '{sanitizedBranchName}' created successfully.");
        }

        private async Task CreateFile(string fileName, string content, string branchName)
        {
            var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/contents/src/_posts/{fileName}";

            // Base64 encode the file content
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
                throw new Exception($"Error creating the file '{fileName}': {errorMessage}");
            }

            Console.WriteLine($"File '{fileName}' created successfully.");
        }

        private async Task<bool> PackagedPullRequest(string branchName, string title)
        {
            var url = $"https://api.github.com/repos/{_userRepoOwner}/{_orgRepoName}/pulls";

            var data = new
            {
                title = title,
                head = $"{_userRepoOwner}:{branchName}", // Specigy the forked repo and the branch
                @base = _defaultBranch, // The branch to merge into (usually "main")
                body = "A new post for review."
            };

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error creating Pull Request: {errorMessage}");
                return false;
            }

            return response.IsSuccessStatusCode;
        }
    }
}
