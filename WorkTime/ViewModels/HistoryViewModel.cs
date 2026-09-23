using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using WorkTime.Models;
using WorkTime.Services;

namespace WorkTime.ViewModels;

public sealed class HistoryViewModel :
    INotifyPropertyChanged
{
    private readonly TimeAggregationService
        _aggregationService;

    private Project? _selectedProject;

    private DateTime _selectedDate =
        DateTime.Today;

    private HistoryPeriod _selectedPeriod =
        HistoryPeriod.Day;

    private HistorySummary? _summary;

    public HistoryViewModel(
        TimeAggregationService aggregationService,
        IEnumerable<Project> projects,
        Project? selectedProject)
    {
        _aggregationService = aggregationService;

        Projects = projects.ToList();

        _selectedProject =
            selectedProject ??
            Projects.FirstOrDefault();
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    public List<Project> Projects { get; }

    public Project? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (_selectedProject?.Id == value?.Id)
            {
                return;
            }

            _selectedProject = value;
            OnPropertyChanged();
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            var date = value.Date;

            if (_selectedDate == date)
            {
                return;
            }

            _selectedDate = date;

            OnPropertyChanged();
            OnPropertyChanged(nameof(PeriodText));
        }
    }

    public HistoryPeriod SelectedPeriod
    {
        get => _selectedPeriod;
        private set
        {
            if (_selectedPeriod == value)
            {
                return;
            }

            _selectedPeriod = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(PeriodText));
        }
    }

    public bool IsDay =>
        SelectedPeriod == HistoryPeriod.Day;

    public bool IsWeek =>
        SelectedPeriod == HistoryPeriod.Week;

    public bool IsMonth =>
        SelectedPeriod == HistoryPeriod.Month;

    public string PeriodText
    {
        get
        {
            var (start, end) =
                TimeAggregationService.GetPeriodRange(
                    SelectedDate,
                    SelectedPeriod);

            return SelectedPeriod switch
            {
                HistoryPeriod.Day =>
                    start.ToString("dd.MM.yyyy"),

                HistoryPeriod.Week =>
                    $"{start:dd.MM.yyyy} — " +
                    $"{end.AddDays(-1):dd.MM.yyyy}",

                HistoryPeriod.Month =>
                    CultureInfo.CurrentCulture
                        .DateTimeFormat
                        .GetMonthName(start.Month) +
                    $" {start.Year}",

                _ => string.Empty
            };
        }
    }

    public string WorkedTimeText =>
        FormatTime(
            _summary?.WorkedTime ??
            TimeSpan.Zero);

    public string PauseTimeText =>
        FormatTime(
            _summary?.PauseTime ??
            TimeSpan.Zero);

    public string HourlyRateText =>
        $"{(_summary?.HourlyRate ?? 0):N2} €/ч";

    public string AmountText =>
        $"{(_summary?.Amount ?? 0):N2} €";

    public async Task LoadAsync()
    {
        await RefreshAsync();
    }

    public async Task SetDayAsync()
    {
        SelectedPeriod = HistoryPeriod.Day;
        NotifyPeriodButtons();
        await RefreshAsync();
    }

    public async Task SetWeekAsync()
    {
        SelectedPeriod = HistoryPeriod.Week;
        NotifyPeriodButtons();
        await RefreshAsync();
    }

    public async Task SetMonthAsync()
    {
        SelectedPeriod = HistoryPeriod.Month;
        NotifyPeriodButtons();
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (SelectedProject is null)
        {
            _summary = null;
            NotifySummary();
            return;
        }

        _summary =
            await _aggregationService
                .GetHistorySummaryAsync(
                    SelectedProject.Id,
                    SelectedDate,
                    SelectedPeriod);

        OnPropertyChanged(nameof(PeriodText));
        NotifySummary();
    }

    private void NotifyPeriodButtons()
    {
        OnPropertyChanged(nameof(IsDay));
        OnPropertyChanged(nameof(IsWeek));
        OnPropertyChanged(nameof(IsMonth));
        OnPropertyChanged(nameof(PeriodText));
    }

    private void NotifySummary()
    {
        OnPropertyChanged(nameof(WorkedTimeText));
        OnPropertyChanged(nameof(PauseTimeText));
        OnPropertyChanged(nameof(HourlyRateText));
        OnPropertyChanged(nameof(AmountText));
    }

    private static string FormatTime(
        TimeSpan time)
    {
        return
            $"{(int)time.TotalHours:00}:" +
            $"{time.Minutes:00}:" +
            $"{time.Seconds:00}";
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