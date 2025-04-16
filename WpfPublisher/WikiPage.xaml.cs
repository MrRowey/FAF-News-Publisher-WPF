using System.Windows;
using System.Windows.Controls;

namespace WpfPublisher
{
    /// <summary>
    /// Interaction logic for WikiPage.xaml
    /// </summary>
    public partial class WikiPage : Page
    {
        public WikiPage()
        {
            InitializeComponent();
        }

        private void TopicsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TopicsList.SelectedItem is ListBoxItem selectedItem)
            {
                string topic = selectedItem.Tag.ToString();
                LoadContent(topic);
            }
        }

        private void LoadContent(string topic)
        {
            // Load content dynamically based on the topic
            switch (topic)
            {
                case "GettingStarted":
                    ContentText.Text = "Welcome to the FAF News Publisher! This app helps you create and manage posts for the FAForever website.";
                    break;
                case "CreatingPosts":
                    ContentText.Text = "To create a post, navigate to the 'Posts' section, fill out the form, and submit your post for review.";
                    break;
                case "SettingsOverview":
                    ContentText.Text = "The 'Settings' page allows you to configure app preferences, such as themes and GitHub authentication.";
                    break;
                default:
                    ContentText.Text = "Select a topic from the left to learn more.";
                    break;
            }
        }
    }
}