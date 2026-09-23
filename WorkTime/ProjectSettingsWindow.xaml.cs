using System.Globalization;
using System.Windows;
using WorkTime.Models;

namespace WorkTime;

public partial class ProjectSettingsWindow : Window
{
    public ProjectSettingsWindow(Project project)
    {
        InitializeComponent();

        NameTextBox.Text = project.Name;

        HourlyRateTextBox.Text =
            project.HourlyRate.ToString(
                "0.##",
                CultureInfo.CurrentCulture);

        AgreedPriceTextBox.Text =
            project.AgreedPrice?.ToString(
                "0.##",
                CultureInfo.CurrentCulture)
            ?? string.Empty;

        DailyTargetTextBox.Text =
            project.DailyTargetMinutes?.ToString()
            ?? string.Empty;

        ReminderBeforeTextBox.Text =
            project.ReminderBeforeMinutes?.ToString()
            ?? string.Empty;

        NotificationsCheckBox.IsChecked =
            project.NotificationsEnabled;
    }

    public string ProjectName { get; private set; } =
        string.Empty;

    public decimal HourlyRate { get; private set; }

    public decimal? AgreedPrice { get; private set; }

    public int? DailyTargetMinutes { get; private set; }

    public int? ReminderBeforeMinutes { get; private set; }

    public bool NotificationsEnabled { get; private set; }


    private void Save_Click(
        object sender,
        RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(
                this,
                "Введите название проекта.",
                "WorkTime",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

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
                this,
                "Введите корректную ставку.",
                "WorkTime",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        decimal? agreedPrice = null;

        if (!string.IsNullOrWhiteSpace(
                AgreedPriceTextBox.Text))
        {
            if (!decimal.TryParse(
                    AgreedPriceTextBox.Text,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out var value) ||
                value < 0)
            {
                MessageBox.Show(
                    this,
                    "Введите корректную договорную цену.",
                    "WorkTime",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            agreedPrice = value;
        }

        int? dailyTarget = null;

        if (!string.IsNullOrWhiteSpace(
                DailyTargetTextBox.Text))
        {
            if (!int.TryParse(
                    DailyTargetTextBox.Text,
                    out var value) ||
                value <= 0)
            {
                MessageBox.Show(
                    this,
                    "Дневная цель должна быть больше 0 минут.",
                    "WorkTime",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            dailyTarget = value;
        }

        int? reminderBefore = null;

        if (!string.IsNullOrWhiteSpace(
                ReminderBeforeTextBox.Text))
        {
            if (!int.TryParse(
                    ReminderBeforeTextBox.Text,
                    out var value) ||
                value < 0)
            {
                MessageBox.Show(
                    this,
                    "Время предупреждения не может быть отрицательным.",
                    "WorkTime",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            reminderBefore = value;
        }

        ProjectName = name;
        HourlyRate = hourlyRate;
        AgreedPrice = agreedPrice;
        DailyTargetMinutes = dailyTarget;
        ReminderBeforeMinutes = reminderBefore;
        NotificationsEnabled =
            NotificationsCheckBox.IsChecked == true;

        DialogResult = true;
    }


    private void Cancel_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}