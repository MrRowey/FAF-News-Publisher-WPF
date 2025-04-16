using System.Configuration;
using System.Data;
using System.Windows;

namespace WpfPublisher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        // Enable or disable debug Mode
        public static bool IsDebugMode { get; } = true; // Set to true for debug mode, false for release mode
    }

}
