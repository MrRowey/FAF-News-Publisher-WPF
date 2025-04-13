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
            var (forkName, isNewFork) = await _forkManager.EnsureForkExists();

            if (!isNewFork)
                await _forkManager.UpdateForkMainBranch();


            var commitSha = await _branchManager.GetLatestCommitSha();
            var branchName = $"post/{title.Replace(" ", "-")}";
            await _branchManager.CreateNewBranch(commitSha, branchName);

            var fileName = $"{DateTime.Now:yyyy-MM-dd}-{title.Replace(" ", "-")}.md";
            var fileContent = $"---\ntitle: {title}\ndate: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nlayout: post\n---\n\n{content}";
            await _fileManager.CreateFile(fileName, fileContent, branchName);

            await _pullRequestManager.PackagedPullRequest(branchName, title);

        }
    }
}