using System.Media;
using System.Windows;
using WorkTime.Views;

namespace WorkTime.Services;

public sealed class NotificationService
{
    private readonly HashSet<string> _shownNotifications = [];

    public void ShowDailyTargetReminder(
        int projectId,
        string projectName,
        DateTime date,
        int remainingMinutes,
        TimeSpan workedTime,
        int targetMinutes)
    {
        var key = $"reminder:{projectId}:{date:yyyy-MM-dd}";

        if (!_shownNotifications.Add(key))
        {
            return;
        }

        ShowWindow(
            projectName,
            $"До дневной цели осталось {remainingMinutes} мин.",
            $"Сегодня: {FormatDuration(workedTime)} из {FormatDuration(TimeSpan.FromMinutes(targetMinutes))}",
            "⏱");
    }

    public void ShowDailyTargetReached(
        int projectId,
        string projectName,
        DateTime date,
        TimeSpan workedTime)
    {
        var key = $"target:{projectId}:{date:yyyy-MM-dd}";

        if (!_shownNotifications.Add(key))
        {
            return;
        }

        ShowWindow(
            projectName,
            "Дневная цель выполнена",
            $"Сегодня: {FormatDuration(workedTime)}",
            "✓");
    }

    private static void ShowWindow(
        string projectName,
        string message,
        string details,
        string icon)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            SystemSounds.Asterisk.Play();

            var window = new NotificationWindow(
                projectName,
                message,
                details,
                icon);

            window.ShowDialog();
        });
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var hours = (int)duration.TotalHours;
        return $"{hours} ч {duration.Minutes:00} мин";
    }
}