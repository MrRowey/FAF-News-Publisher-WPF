using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfPublisher.Core;

namespace WpfPublisher
{
    /// <summary>
    /// Interaction logic for PostPage.xaml
    /// </summary>
    public partial class PostPage : Page
    {

        private  readonly GitHubPullRequest _pullRequest;
        private string _accessToken;
        private string _userRepoOwner = "MrRowey"; // Replace with your GitHub username

        public PostPage(string accessToken)
        {
            InitializeComponent();
            _accessToken = accessToken;
            _pullRequest = new GitHubPullRequest(_accessToken, _userRepoOwner );
        }


        private async void SubmitPost_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string title = TitleBox.Text.Trim();
                string content = ContentBox.Text.Trim();

                await _pullRequest.CreatePullRequest(title, content);
                StatusText.Text = "Post submitted for review!";
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the post submission process
                StatusText.Text = "Error: " + ex.Message;
                StatusText.Foreground = Brushes.Red;
            }
        }
    }
}
