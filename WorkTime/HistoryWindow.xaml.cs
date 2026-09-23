using System.Windows;
using System.Windows.Controls;
using WorkTime.ViewModels;

namespace WorkTime;

public partial class HistoryWindow : Window
{
    public HistoryWindow()
    {
        InitializeComponent();
    }

    private HistoryViewModel ViewModel =>
        (HistoryViewModel)DataContext;

    private async void Project_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private async void DatePicker_SelectedDateChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        await ViewModel.RefreshAsync();
    }

    private async void Day_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SetDayAsync();
    }

    private async void Week_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SetWeekAsync();
    }

    private async void Month_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SetMonthAsync();
    }
}