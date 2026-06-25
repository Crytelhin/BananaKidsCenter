using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRussianSelected))]
    [NotifyPropertyChangedFor(nameof(IsRomanianSelected))]
    [NotifyPropertyChangedFor(nameof(IsEnglishSelected))]
    private string selectedLanguage = "Русский";

    public bool IsRussianSelected => SelectedLanguage == "Русский";
    public bool IsRomanianSelected => SelectedLanguage == "Română";
    public bool IsEnglishSelected => SelectedLanguage == "English";

    public List<string> Languages { get; } = ["Русский", "Română", "English"];

    private bool _isInitializing = true;

    public SettingsViewModel()
    {
        SelectedLanguage = Preferences.Get("AppLanguage", "Русский");
        _isInitializing = false;
    }

    [RelayCommand]
    private void ChangeLanguage(string language)
    {
        SelectedLanguage = language;
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        if (_isInitializing) return;

        Preferences.Set("AppLanguage", value);
        
        var culture = value switch
        {
            "Română" => new System.Globalization.CultureInfo("ro"),
            "English" => new System.Globalization.CultureInfo("en"),
            _ => new System.Globalization.CultureInfo("ru")
        };
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Application.Current != null)
            {
                Application.Current.MainPage = new AppShell();
            }
        });
    }
}
