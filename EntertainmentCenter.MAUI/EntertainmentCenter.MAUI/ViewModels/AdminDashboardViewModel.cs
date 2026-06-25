using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Models;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class AdminDashboardViewModel : ObservableObject
{
    private readonly AdminApiService _adminService;

    [ObservableProperty]
    private int visitsToday;

    [ObservableProperty]
    private int activeNow;

    [ObservableProperty]
    private decimal revenueToday;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private DateTime exportDate = DateTime.Today;

    [ObservableProperty]
    private string reportLanguage;

    [ObservableProperty]
    private string reportPeriod;

    [ObservableProperty]
    private int selectedMonthIndex = DateTime.Today.Month - 1;

    [ObservableProperty]
    private int selectedYear = DateTime.Today.Year;

    public List<string> ReportLanguages { get; } = ["🇷🇺 Русский", "🇷🇴 Română", "🇬🇧 English"];

    public List<string> ReportPeriods => [AppResources.ReportDay, AppResources.ReportWeek, AppResources.ReportMonth, AppResources.ReportYear];

    public List<string> Months { get; private set; } = [];
    public List<int> Years { get; private set; } = [];

    public bool IsDatePickerVisible => ReportPeriod == AppResources.ReportDay || ReportPeriod == AppResources.ReportWeek;
    public bool IsMonthPickerVisible => ReportPeriod == AppResources.ReportMonth;
    public bool IsYearPickerVisible => ReportPeriod == AppResources.ReportYear;

    public AdminDashboardViewModel(AdminApiService adminService)
    {
        _adminService = adminService;
        var appLang = Preferences.Get("AppLanguage", "Русский");
        ReportLanguage = appLang switch
        {
            "Română" => "🇷🇴 Română",
            "English" => "🇬🇧 English",
            _ => "🇷🇺 Русский"
        };
        ReportPeriod = AppResources.ReportDay;
        LoadMonthNames();
        LoadYears();
    }

    partial void OnReportPeriodChanged(string value)
    {
        OnPropertyChanged(nameof(IsDatePickerVisible));
        OnPropertyChanged(nameof(IsMonthPickerVisible));
        OnPropertyChanged(nameof(IsYearPickerVisible));
        UpdateExportDateForPeriod();
    }

    partial void OnSelectedMonthIndexChanged(int value) => UpdateExportDateForPeriod();
    partial void OnSelectedYearChanged(int value) => UpdateExportDateForPeriod();

    private string GetReportLanguageCode() => ReportLanguage switch
    {
        "🇷🇴 Română" => "ro",
        "🇬🇧 English" => "en",
        _ => "ru"
    };

    private string GetReportPeriodCode() => ReportPeriod switch
    {
        var p when p == AppResources.ReportWeek => "week",
        var p when p == AppResources.ReportMonth => "month",
        var p when p == AppResources.ReportYear => "year",
        _ => "day"
    };

    private void UpdateExportDateForPeriod()
    {
        var periodCode = GetReportPeriodCode();
        if (periodCode == "month")
            ExportDate = new DateTime(SelectedYear, SelectedMonthIndex + 1, 1);
        else if (periodCode == "year")
            ExportDate = new DateTime(SelectedYear, 1, 1);
    }

    private void LoadMonthNames()
    {
        var appLang = Preferences.Get("AppLanguage", "Русский");
        var culture = appLang switch
        {
            "Română" => new CultureInfo("ro-RO"),
            "English" => new CultureInfo("en-US"),
            _ => new CultureInfo("ru-RU")
        };
        Months = culture.DateTimeFormat.MonthNames
            .Take(12)
            .Select(m => string.IsNullOrEmpty(m) ? "" : char.ToUpper(m[0]) + m[1..])
            .ToList();
    }

    private void LoadYears()
    {
        var currentYear = DateTime.Today.Year;
        Years = Enumerable.Range(currentYear - 3, 7).ToList();
    }

    [RelayCommand]
    private async Task ExportReport()
    {
        IsBusy = true;
        try
        {
            var lang = GetReportLanguageCode();
            var period = GetReportPeriodCode();
            var csvBytes = await _adminService.GetDailyReportCsvAsync(ExportDate, lang, period);
            if (csvBytes == null || csvBytes.Length == 0)
            {
                await Shell.Current.DisplayAlert(AppResources.Info, AppResources.NoReportData, "OK");
                return;
            }

            var fileName = period switch
            {
                "week" => $"weekly-report-{ExportDate:yyyy-MM-dd}.csv",
                "month" => $"monthly-report-{ExportDate:yyyy-MM}.csv",
                "year" => $"yearly-report-{ExportDate:yyyy}.csv",
                _ => $"daily-report-{ExportDate:yyyy-MM-dd}.csv"
            };
            var tempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(tempFilePath, csvBytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = string.Format(AppResources.ReportTitleFormat, ExportDate),
                File = new ShareFile(tempFilePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, string.Format(AppResources.ExportReportError, ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadMetrics()
    {
        IsBusy = true;
        try
        {
            var metrics = await _adminService.GetDashboardAsync();
            if (metrics != null)
            {
                VisitsToday = metrics.VisitsToday;
                ActiveNow = metrics.ActiveNow;
                RevenueToday = metrics.RevenueToday;
            }
        }
        catch (HttpRequestException)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.NoConnectionToServer, "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ServerError, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToZones()
    {
        await Shell.Current.GoToAsync("ZonesListPage");
    }

    [RelayCommand]
    private async Task GoToPromotions()
    {
        await Shell.Current.GoToAsync("PromotionsListPage");
    }

    [RelayCommand]
    private async Task GoToClientHistory()
    {
        await Shell.Current.GoToAsync("ClientHistoryPage");
    }

    [RelayCommand]
    private async Task GoToNotifications()
    {
        await Shell.Current.GoToAsync("NotificationSettingsPage");
    }

    [RelayCommand]
    private async Task GoToServerConnection()
    {
        await Shell.Current.GoToAsync("ServerConnectionPage");
    }

    [RelayCommand]
    private async Task Logout()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            AppResources.ExitConfirmTitle,
            AppResources.ExitConfirmMessage,
            AppResources.LogoutButton, AppResources.CancelButton);

        if (confirm)
            await Shell.Current.GoToAsync("../..");
    }
}
