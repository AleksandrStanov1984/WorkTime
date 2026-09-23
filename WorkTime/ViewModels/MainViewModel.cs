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
    private readonly ProjectAnalyticsService _analyticsService;
    private readonly NotificationService _notificationService;

    private Project? _selectedProject;
    private TimeSpan _todayWorkedTime;
    private ProjectAnalytics? _projectAnalytics;

    public MainViewModel(
    ProjectService projectService,
    WorkTimerService timerService,
    TimeAggregationService aggregationService,
    ProjectAnalyticsService analyticsService,
    NotificationService notificationService)
    {
        _projectService = projectService;
        _timerService = timerService;
        _aggregationService = aggregationService;
        _analyticsService = analyticsService;
        _notificationService = notificationService;
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

    public ProjectAnalytics? ProjectAnalytics
    {
        get => _projectAnalytics;
        private set
        {
            _projectAnalytics = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(ProjectWorkedTimeText));
            OnPropertyChanged(nameof(EarnedAmountText));
            OnPropertyChanged(nameof(HourlyRateText));
            OnPropertyChanged(nameof(AgreedPriceText));
            OnPropertyChanged(nameof(RemainingAmountText));
            OnPropertyChanged(nameof(EffectiveRateText));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(HasAgreedPrice));
        }
    }

    public string ProjectWorkedTimeText =>
        ProjectAnalytics is null
            ? "00:00"
            : $"{(int)ProjectAnalytics.WorkedTime.TotalHours:00}:" +
              $"{ProjectAnalytics.WorkedTime.Minutes:00}";

    public string EarnedAmountText =>
        ProjectAnalytics is null
            ? "€0,00"
            : $"€{ProjectAnalytics.EarnedAmount:N2}";

    public string HourlyRateText =>
   ProjectAnalytics is null
       ? "€0,00/ч"
       : $"€{ProjectAnalytics.HourlyRate:N2}/ч";

    public string AgreedPriceText =>
        ProjectAnalytics?.AgreedPrice is null
            ? "Не задана"
            : $"€{ProjectAnalytics.AgreedPrice.Value:N2}";

    public string RemainingAmountText =>
        ProjectAnalytics?.RemainingAmount is null
            ? "—"
            : $"€{ProjectAnalytics.RemainingAmount.Value:N2}";

    public string EffectiveRateText =>
        ProjectAnalytics?.EffectiveHourlyRate is null
            ? "—"
            : $"€{ProjectAnalytics.EffectiveHourlyRate.Value:N2}/ч";

    public string ProgressText =>
        ProjectAnalytics?.ProgressPercent is null
            ? "—"
            : $"{ProjectAnalytics.ProgressPercent.Value:N1}%";

    public bool HasAgreedPrice =>
        ProjectAnalytics?.AgreedPrice is not null;

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
        await RefreshProjectAnalyticsAsync();

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
        await RefreshProjectAnalyticsAsync();

        NotifyTimerStateChanged();
    }

    public async Task PauseAsync()
    {
        await _timerService.PauseAsync();

        await RefreshTodayAsync();
        await RefreshProjectAnalyticsAsync();

        NotifyTimerStateChanged();
    }

    public async Task ResumeAsync()
    {
        await _timerService.ResumeAsync();

        await RefreshTodayAsync();
        await RefreshProjectAnalyticsAsync();

        NotifyTimerStateChanged();
    }

    public async Task FinishAsync()
    {
        await _timerService.FinishAsync();

        await RefreshTodayAsync();
        await RefreshProjectAnalyticsAsync();

        NotifyTimerStateChanged();
    }

    public async Task SwitchProjectAsync(
    Project project)
    {
        if (_timerService.State !=
            WorkTimerState.Running)
        {
            SelectedProject = project;

            await RefreshProjectAnalyticsAsync();

            return;
        }

        if (_timerService.ActiveProjectId ==
            project.Id)
        {
            SelectedProject = project;

            await RefreshProjectAnalyticsAsync();

            return;
        }

        await _timerService.SwitchProjectAsync(
            project.Id);

        SelectedProject = project;

        await RefreshTodayAsync();
        await RefreshProjectAnalyticsAsync();

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

        await RefreshProjectAnalyticsAsync();
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

        var project =
            await _projectService.UpdateAsync(
                SelectedProject.Id,
                name,
                hourlyRate,
                agreedPrice,
                dailyTargetMinutes,
                reminderBeforeMinutes,
                notificationsEnabled);

        SelectedProject = project;

        OnPropertyChanged(nameof(Projects));

        await RefreshProjectAnalyticsAsync();
    }

    public async Task RefreshTodayAsync()
    {
        TodayWorkedTime =
            await _aggregationService
                .GetWorkedTimeForDayAsync(
                    DateTime.Today);
    }

    public async Task RefreshProjectAnalyticsAsync()
    {
        if (SelectedProject is null)
        {
            ProjectAnalytics = null;
            return;
        }

        ProjectAnalytics =
            await _analyticsService.GetAsync(
                SelectedProject.Id);
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

    public async Task CheckDailyTargetAsync()
    {
        if (_timerService.State != WorkTimerState.Running ||
            _timerService.ActiveProjectId is null)
        {
            return;
        }

        var project = Projects.FirstOrDefault(
            x => x.Id == _timerService.ActiveProjectId.Value);

        if (project is null ||
            !project.NotificationsEnabled ||
            project.DailyTargetMinutes is null ||
            project.DailyTargetMinutes <= 0)
        {
            return;
        }

        var workedTime =
            await _aggregationService.GetWorkedTimeForProjectDayAsync(
                project.Id,
                DateTime.Today);

        var workedMinutes =
            workedTime.TotalMinutes;

        var targetMinutes =
            project.DailyTargetMinutes.Value;

        if (workedMinutes >= targetMinutes)
        {
            _notificationService.ShowDailyTargetReached(
                project.Id,
                project.Name,
                DateTime.Today,
                workedTime);

            return;
        }

        if (project.ReminderBeforeMinutes is null ||
            project.ReminderBeforeMinutes <= 0)
        {
            return;
        }

        var reminderMinutes =
            project.ReminderBeforeMinutes.Value;

        if (workedMinutes >=
            targetMinutes - reminderMinutes)
        {
            _notificationService.ShowDailyTargetReminder(
                project.Id,
                project.Name,
                DateTime.Today,
                reminderMinutes,
                workedTime,
                targetMinutes);
        }
    }

    public async Task DeleteSelectedProjectAsync()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var projectId = SelectedProject.Id;

        if (_timerService.ActiveProjectId == projectId &&
            _timerService.State != WorkTimerState.Stopped)
        {
            await _timerService.FinishAsync();
        }

        await _projectService.DeleteAsync(projectId);

        Projects =
            await _projectService.GetActiveProjectsAsync();

        OnPropertyChanged(nameof(Projects));

        SelectedProject =
            Projects.FirstOrDefault();

        await RefreshTodayAsync();
        await RefreshProjectAnalyticsAsync();

        NotifyTimerStateChanged();
    }
}