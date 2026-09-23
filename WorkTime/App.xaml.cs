using System.Windows;
using WorkTime.Data;
using WorkTime.Services;
using WorkTime.ViewModels;

namespace WorkTime;

public partial class App : Application
{
    private WorkTimeDbContext? _dbContext;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _dbContext = new WorkTimeDbContext();

        await _dbContext.Database.EnsureCreatedAsync();

        var clock = new SystemClock();

        var projectService =
            new ProjectService(_dbContext);

        var timerService =
            new WorkTimerService(_dbContext, clock);

        var aggregationService =
            new TimeAggregationService(_dbContext, clock);

        var viewModel = new MainViewModel(
            projectService,
            timerService,
            aggregationService);

        await viewModel.InitializeAsync();

        var window = new MainWindow
        {
            DataContext = viewModel
        };

        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _dbContext?.Dispose();

        base.OnExit(e);
    }
}
