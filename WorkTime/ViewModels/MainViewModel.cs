using System.ComponentModel;
using System.Runtime.CompilerServices;
using WorkTime.Models;
using WorkTime.Services;

namespace WorkTime.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ProjectService _projectService;
    private readonly WorkTimerService _timerService;
    private readonly TimeAggregationService _aggregationService;

    private Project? _selectedProject;
    private TimeSpan _todayWorkedTime;

    public MainViewModel(
        ProjectService projectService,
        WorkTimerService timerService,
        TimeAggregationService aggregationService)
    {
        _projectService = projectService;
        _timerService = timerService;
        _aggregationService = aggregationService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public List<Project> Projects { get; private set; } = [];

    public Project? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (_selectedProject == value)
            {
                return;
            }

            _selectedProject = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan TodayWorkedTime
    {
        get => _todayWorkedTime;
        private set
        {
            if (_todayWorkedTime == value)
            {
                return;
            }

            _todayWorkedTime = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(TodayWorkedTimeText));
        }
    }

    public string TodayWorkedTimeText =>
        $"{(int)TodayWorkedTime.TotalHours:00}:" +
        $"{TodayWorkedTime.Minutes:00}:" +
        $"{TodayWorkedTime.Seconds:00}";

    public WorkTimerState TimerState =>
        _timerService.State;

    public bool IsRunning =>
        TimerState == WorkTimerState.Running;

    public bool IsPaused =>
        TimerState == WorkTimerState.Paused;

    public bool IsStopped =>
        TimerState == WorkTimerState.Stopped;

    public bool CanFinish =>
        TimerState == WorkTimerState.Running ||
        TimerState == WorkTimerState.Paused;

    public async Task InitializeAsync()
    {
        await _timerService.RestoreAsync();

        Projects =
            await _projectService.GetActiveProjectsAsync();

        OnPropertyChanged(nameof(Projects));

        if (_timerService.ActiveProjectId is not null)
        {
            SelectedProject =
                Projects.FirstOrDefault(
                    x => x.Id ==
                         _timerService.ActiveProjectId);
        }
        else
        {
            SelectedProject =
                Projects.FirstOrDefault();
        }

        await RefreshTodayAsync();

        NotifyTimerStateChanged();
    }

    public async Task StartAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        await _timerService.StartAsync(
            SelectedProject.Id);

        await RefreshTodayAsync();

        NotifyTimerStateChanged();
    }

    public async Task PauseAsync()
    {
        await _timerService.PauseAsync();

        await RefreshTodayAsync();

        NotifyTimerStateChanged();
    }

    public async Task ResumeAsync()
    {
        await _timerService.ResumeAsync();

        await RefreshTodayAsync();

        NotifyTimerStateChanged();
    }

    public async Task FinishAsync()
    {
        await _timerService.FinishAsync();

        await RefreshTodayAsync();

        NotifyTimerStateChanged();
    }

    public async Task SwitchProjectAsync(
        Project project)
    {
        if (_timerService.State !=
            WorkTimerState.Running)
        {
            SelectedProject = project;
            return;
        }

        if (_timerService.ActiveProjectId ==
            project.Id)
        {
            SelectedProject = project;
            return;
        }

        await _timerService.SwitchProjectAsync(
            project.Id);

        SelectedProject = project;

        await RefreshTodayAsync();

        NotifyTimerStateChanged();
    }

    public async Task CreateProjectAsync(
        string name,
        decimal hourlyRate)
    {
        var project =
            await _projectService.CreateAsync(
                name,
                hourlyRate);

        Projects =
            await _projectService.GetActiveProjectsAsync();

        OnPropertyChanged(nameof(Projects));

        SelectedProject =
            Projects.First(
                x => x.Id == project.Id);
    }

    public async Task UpdateSelectedProjectAsync(
    string name,
    decimal hourlyRate,
    decimal? agreedPrice,
    int? dailyTargetMinutes,
    int? reminderBeforeMinutes,
    bool notificationsEnabled)
    {
        if (SelectedProject is null)
        {
            return;
        }

        var project = await _projectService.UpdateAsync(
            SelectedProject.Id,
            name,
            hourlyRate,
            agreedPrice,
            dailyTargetMinutes,
            reminderBeforeMinutes,
            notificationsEnabled);

        SelectedProject = project;

        OnPropertyChanged(nameof(Projects));
    }

    public async Task RefreshTodayAsync()
    {
        TodayWorkedTime =
            await _aggregationService
                .GetWorkedTimeForDayAsync(
                    DateTime.Today);
    }

    public HistoryViewModel CreateHistoryViewModel()
    {
        return new HistoryViewModel(
            _aggregationService,
            Projects,
            SelectedProject);
    }

    private void NotifyTimerStateChanged()
    {
        OnPropertyChanged(nameof(TimerState));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsStopped));
        OnPropertyChanged(nameof(CanFinish));
    }

    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}