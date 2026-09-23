using System.Globalization;
using System.Windows;

namespace WorkTime;

public partial class NewProjectWindow : Window
{
    public NewProjectWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            NameTextBox.Focus();
        };
    }

    public string ProjectName =>
        NameTextBox.Text.Trim();

    public decimal HourlyRate { get; private set; }

    private void Create_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            MessageBox.Show(
                "Введите название проекта.",
                "WorkTime",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        if (!decimal.TryParse(
                HourlyRateTextBox.Text,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var hourlyRate) ||
            hourlyRate < 0)
        {
            MessageBox.Show(
                "Введите корректную ставку.",
                "WorkTime",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        HourlyRate = hourlyRate;
        DialogResult = true;
    }

    private void Cancel_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}