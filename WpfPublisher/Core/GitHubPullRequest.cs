using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using System.Text;

namespace WpfPublisher.Core
{
    public class GitHubPullRequest(string accessToken)
    {
        private readonly string _accessToken = accessToken;
        private readonly string _repoOwner = "MrRowey";
        private readonly string _repoName = "faf-new-website";
        private readonly string _defaultBranch = "main"; // Usually "main"

        public async Task CreatePullRequest(string title, string content)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "token " + _accessToken);
            client.DefaultRequestHeaders.Add("User-Agent", "JekyllPublisher");

            // Step 1: Get the latest commit SHA from the default branch (e.g., 'main')
            var commitSha = await GetLatestCommitSha(client);

            // Step 2: Create a new branch from the latest commit SHA
            var newBranchName = $"post/{title.Replace(" ", "-")}";
            await CreateNewBranch(client, commitSha, newBranchName);

            // Step 3: Create the markdown file in the '_posts' folder
            var fileName = $"{DateTime.Now:yyyy-MM-dd}-{title.Replace(" ", "-")}.md";
            var fileContent = $"---\ntitle: {title}\ndate: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nlayout: post\n---\n\n{content}";
            await CreateFile(client, fileName, fileContent, newBranchName);

            // Step 4: Create a Pull Request
            var prCreated = await CreatePullRequest(client, newBranchName, title);

            if (prCreated)
            {
                Console.WriteLine("Your post has been submitted for review!");
            }
            else
            {
                Console.WriteLine("Error creating Pull Request.");
            }
        }

        private async Task<string> GetLatestCommitSha(HttpClient client)
        {
            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/commits/{_defaultBranch}";
            var response = await client.GetStringAsync(url);
            dynamic commit = JsonConvert.DeserializeObject(response);
            return commit.sha;
        }

        private async Task CreateNewBranch(HttpClient client, string commitSha, string branchName)
        {
            // Sanitize the branch name to remove special characters
            var sanitizedBranchName = branchName.Replace(" ", "-");

            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/git/refs";

            // Prepare the data for the new branch
            var data = new
            {
                @ref = $"refs/heads/{sanitizedBranchName}", // Refers to the new branch we want to create
                sha = commitSha // Pointing the new branch to the SHA of the latest commit from the default branch
            };

            // Convert the data to JSON
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // GitHub requires a User-Agent header
            client.DefaultRequestHeaders.Add("User-Agent", "JekyllPublisher");

            // Make the POST request to create the new branch
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating the new branch: {errorMessage}");
            }

            Console.WriteLine($"Branch '{sanitizedBranchName}' created successfully.");
        }

        private async Task CreateFile(HttpClient client, string fileName, string content, string branchName)
        {
            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/contents/src/_posts/{fileName}";

            // Base64 encode the file content
            var data = new
            {
                message = $"Add new post: {fileName}",
                content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                branch = branchName
            };

            var json = JsonConvert.SerializeObject(data);
            var contentData = new StringContent(json, Encoding.UTF8, "application/json");

            // GitHub requires a User-Agent header
            client.DefaultRequestHeaders.Add("User-Agent", "JekyllPublisher");

            var response = await client.PutAsync(url, contentData);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating the file '{fileName}': {errorMessage}");
            }

            Console.WriteLine($"File '{fileName}' created successfully.");
        }

        private async Task<bool> CreatePullRequest(HttpClient client, string branchName, string title)
        {
            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/pulls";

            var data = new
            {
                title = title,
                head = branchName, // The new branch to create the PR from
                @base = _defaultBranch, // The branch to merge into (usually "main")
                body = "A new post for review."
            };

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // GitHub requires a User-Agent header
            client.DefaultRequestHeaders.Add("User-Agent", "JekyllPublisher");

            var response = await client.PostAsync(url, content);

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
