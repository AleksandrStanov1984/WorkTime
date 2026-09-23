using System.Windows;

namespace WorkTime.Views;

public partial class NotificationWindow : Window
{
    public NotificationWindow(
        string projectName,
        string message,
        string details,
        string icon)
    {
        InitializeComponent();

        ProjectNameText.Text = projectName;
        MessageText.Text = message;
        DetailsText.Text = details;
        IconText.Text = icon;

        Loaded += NotificationWindow_Loaded;
    }

    private void NotificationWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Activate();
        Focus();
    }

    private void Ok_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}