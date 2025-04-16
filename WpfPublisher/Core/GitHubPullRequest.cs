using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;

namespace WpfPublisher.Core
{
    public class GitHubPullRequest
    {
        private readonly ForkManager _forkManager;
        private readonly BranchManager _branchManager;
        private readonly FileManager _fileManager;
        private readonly PullRequestManager _pullRequestManager;

        public GitHubPullRequest(string accessToken, string userRepoOwner)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "token " + accessToken);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "JekyllPublisher");

            _forkManager = new ForkManager(httpClient, "FAForever", "website-news", userRepoOwner, "main");
            _branchManager = new BranchManager(httpClient, userRepoOwner, "website-news");
            _fileManager = new FileManager(httpClient, userRepoOwner, "website-news");
            _pullRequestManager = new PullRequestManager(httpClient, "FAForever", "website-news", userRepoOwner);
        }

        public async Task CreatePullRequest(string title, string content)
        {
            // Step 1: Ensure the fork exists
            var (forkName, isNewFork) = await _forkManager.EnsureForkExists();

            // Step 2: Update the main branch of the fork if it's a new fork

            if (!isNewFork)
                await _forkManager.UpdateForkMainBranch();

            // Step 3: Get the latest commit SHA from the main branch of the fork
            var commitSha = await _branchManager.GetLatestCommitSha();

            //Step 4: Create new branch and file
            var branchName = $"post/{title.Replace(" ", "-")}";
            await _branchManager.CreateNewBranch(commitSha, branchName);

            // Step 5: Create the file in the new branch
            var fileName = $"{DateTime.Now:yyyy-MM-dd}-{title.Replace(" ", "-")}.md";
            var fileContent = $"---\ntitle: {title}\ndate: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nlayout: post\n---\n\n{content}";
            await _fileManager.CreateFile(fileName, fileContent, branchName);

            // Step 6: Create the pull request (only in Production)
            if (!App.IsDebugMode)
            {
                bool success = await _pullRequestManager.PackagedPullRequest(branchName, title);
                if (!success)
                {
                    Debug.WriteLine("Failed to create pull request.");
                    throw new Exception("Failed to create pull request.");
                }
            }
            else
            {
                // Debug mode: log the pull request creation
                Debug.WriteLine($"[DEBUG] Pull request steps completed up to branch creation");
                Debug.WriteLine($"[DEBUG] Branch name: {branchName}");
                Debug.WriteLine($"[DEBUG] File name: {fileName}");
            }
        }
    }
}