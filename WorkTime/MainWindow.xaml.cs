using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WorkTime.Models;
using WorkTime.ViewModels;
using Forms = System.Windows.Forms;

namespace WorkTime;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _displayTimer;
    private readonly Forms.NotifyIcon _trayIcon;

    public MainWindow()
    {
        InitializeComponent();

        _displayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _displayTimer.Tick += DisplayTimer_Tick;

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
        Environment.ProcessPath!)
        ?? System.Drawing.SystemIcons.Application,
            Text = "WorkTime — Stopped",
            Visible = true
        };

        var contextMenu = new Forms.ContextMenuStrip();

        contextMenu.Items.Add(
            "Открыть WorkTime",
            null,
            (_, _) => Dispatcher.Invoke(ShowFromTray));

        contextMenu.Items.Add(new Forms.ToolStripSeparator());

        contextMenu.Items.Add(
            "Выход",
            null,
            (_, _) => Dispatcher.Invoke(ExitApplication));

        _trayIcon.ContextMenuStrip = contextMenu;

        _trayIcon.DoubleClick +=
            (_, _) => Dispatcher.Invoke(ShowFromTray);

        Loaded += (_, _) =>
        {
            _displayTimer.Start();
            UpdateTrayState();
        };
    }

    private MainViewModel ViewModel =>
        (MainViewModel)DataContext;

    private async void DisplayTimer_Tick(
        object? sender,
        EventArgs e)
    {
        await ViewModel.RefreshTodayAsync();
        await ViewModel.RefreshProjectAnalyticsAsync();
        await ViewModel.CheckDailyTargetAsync();

        UpdateTrayState();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _displayTimer.Stop();

        _trayIcon.Visible = false;
        _trayIcon.Dispose();

        base.OnClosing(e);
    }

    private void ShowFromTray()
    {
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void ExitApplication()
    {
        Close();
    }

    private void UpdateTrayState()
    {
        _trayIcon.Text = ViewModel.TimerState switch
        {
            WorkTimerState.Running => "WorkTime — Running",
            WorkTimerState.Paused => "WorkTime — Paused",
            _ => "WorkTime — Stopped"
        };
    }

    private async void Start_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.StartAsync();
        UpdateTrayState();
    }

    private async void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.PauseAsync();
        UpdateTrayState();
    }

    private async void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.ResumeAsync();
        UpdateTrayState();
    }

    private async void Finish_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.FinishAsync();
        UpdateTrayState();
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

    private async void ProjectSettings_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ViewModel.SelectedProject is null)
        {
            return;
        }

        var dialog =
            new ProjectSettingsWindow(
                ViewModel.SelectedProject)
            {
                Owner = this
            };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (dialog.DeleteRequested)
        {
            await ViewModel.DeleteSelectedProjectAsync();
            UpdateTrayState();
            return;
        }

        await ViewModel.UpdateSelectedProjectAsync(
            dialog.ProjectName,
            dialog.HourlyRate,
            dialog.AgreedPrice,
            dialog.DailyTargetMinutes,
            dialog.ReminderBeforeMinutes,
            dialog.NotificationsEnabled);
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
        UpdateTrayState();
    }

    private async void History_Click(
        object sender,
        RoutedEventArgs e)
    {
        var viewModel =
            ViewModel.CreateHistoryViewModel();

        await viewModel.LoadAsync();

        var window = new HistoryWindow
        {
            Owner = this,
            DataContext = viewModel
        };

        window.ShowDialog();
    }
}