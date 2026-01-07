using System.Windows;
using System.Windows.Controls;

namespace HashcatGUI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void CopyPassword_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string password)
        {
            try
            {
                Clipboard.SetText(password);
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
