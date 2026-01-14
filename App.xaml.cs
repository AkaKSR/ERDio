using System.Windows;

namespace ERDio;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        this.DispatcherUnhandledException += (s, e) =>
        {
            Console.WriteLine($"ERROR: {e.Exception}");
            MessageBox.Show(e.Exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}

