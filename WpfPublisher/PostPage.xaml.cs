using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                // Retrive the values from the UI
                string title = TitleBox.Text.Trim();
                string date = DatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                string time = TimeBox.Text.Trim();
                string categories = CategoriesBox.Text.Trim();
                string type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? string.Empty;
                string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? string.Empty;
                string priority = PriorityBox.Text.Trim();
                string redirectURL = RedirectURLBox.Text.Trim();
                string imagePath = ImagePathBox.Text.Trim();
                string imageAlt = ImageAltBox.Text.Trim();
                string eventStartDate = EventStartDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                string eventStartTime = EventStartTimeBox.Text.Trim();
                string eventEndDate = EventEndDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                string eventEndTime = EventEndTimeBox.Text.Trim();
                string eventImageURL = EventImageBox.Text.Trim();
                string content = ContentBox.Text.Trim();


                // Construct post Content
                string postContent = $@"
---
layout: post
title: {title}
date: {date} {time}
categories: {categories}
type: {type}
status: {status}
priority: {priority}

redirectURL: {redirectURL}

image:
    path: {imagePath}
    alt: {imageAlt}

event:
    dtstart: {eventStartDate} {eventStartTime}
    dtend: {eventEndDate} {eventEndTime}
    image: {eventImageURL}
---

{content}
";

                // Submit the Post
                await _pullRequest.CreatePullRequest(title, postContent);

                // Show Successs pop-up
                MessageBox.Show("Post submitted for review!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                //Update status text
                Debug.WriteLine("Post submitted for review!");
            }
            catch (Exception ex)
            {
                // Update status text
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Update status text
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
