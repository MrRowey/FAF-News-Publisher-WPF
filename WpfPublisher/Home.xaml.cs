﻿using System;
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

namespace WpfPublisher
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        public Home()
        {
            InitializeComponent();
        }


        private void GetStarted_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to the PostPage the "Getting stared" button is clicked
            NavigationService.Navigate(new PostPage("dummy_accses_token")); // replace with actual token if needed
        }


    }
}
