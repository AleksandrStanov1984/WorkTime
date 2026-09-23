using System.Windows;
using System.Windows.Threading;
using WorkTime.ViewModels;
using System.Windows.Controls;
using WorkTime.Models;


namespace WorkTime;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _displayTimer;

    public MainWindow()
    {
        InitializeComponent();

        _displayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _displayTimer.Tick += DisplayTimer_Tick;

        Loaded += (_, _) =>
        {
            _displayTimer.Start();
        };

        Closed += (_, _) =>
        {
            _displayTimer.Stop();
        };
    }

    private MainViewModel ViewModel =>
        (MainViewModel)DataContext;

    private async void DisplayTimer_Tick(
        object? sender,
        EventArgs e)
    {
        await ViewModel.RefreshTodayAsync();
    }

    private async void Start_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.StartAsync();
    }

    private async void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.PauseAsync();
    }

    private async void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.ResumeAsync();
    }

    private async void Finish_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.FinishAsync();
    }

    private async void NewProject_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new NewProjectWindow
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await ViewModel.CreateProjectAsync(
            dialog.ProjectName,
            dialog.HourlyRate);
    }

    private async void Project_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (sender is not ComboBox comboBox ||
            comboBox.SelectedItem is not Project project)
        {
            return;
        }

        await ViewModel.SwitchProjectAsync(project);
    }
}