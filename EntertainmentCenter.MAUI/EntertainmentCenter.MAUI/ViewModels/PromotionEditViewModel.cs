using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Models;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class PromotionEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly PromotionApiService _promotionService;

    private int _promotionId;

    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private DiscountType discountType = DiscountType.Percent;

    [ObservableProperty]
    private decimal discountValue;

    [ObservableProperty]
    private DayOfWeek? applicableDay;

    [ObservableProperty]
    private bool isPercent = true;

    [ObservableProperty]
    private bool isFixed;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyDaySelected))]
    [NotifyPropertyChangedFor(nameof(IsSundaySelected))]
    [NotifyPropertyChangedFor(nameof(IsMondaySelected))]
    [NotifyPropertyChangedFor(nameof(IsTuesdaySelected))]
    [NotifyPropertyChangedFor(nameof(IsWednesdaySelected))]
    [NotifyPropertyChangedFor(nameof(IsThursdaySelected))]
    [NotifyPropertyChangedFor(nameof(IsFridaySelected))]
    [NotifyPropertyChangedFor(nameof(IsSaturdaySelected))]
    private int selectedDayIndex = -1;

    public bool IsAnyDaySelected => SelectedDayIndex == 0;
    public bool IsSundaySelected => SelectedDayIndex == 1;
    public bool IsMondaySelected => SelectedDayIndex == 2;
    public bool IsTuesdaySelected => SelectedDayIndex == 3;
    public bool IsWednesdaySelected => SelectedDayIndex == 4;
    public bool IsThursdaySelected => SelectedDayIndex == 5;
    public bool IsFridaySelected => SelectedDayIndex == 6;
    public bool IsSaturdaySelected => SelectedDayIndex == 7;

    public List<string> DayNames { get; } =
    [
        AppResources.AnyDay,
        AppResources.Mon, // We will map to resource string or correct naming
        AppResources.Tue,
        AppResources.Wed,
        AppResources.Thu,
        AppResources.Fri,
        AppResources.Sat,
        AppResources.Sun
    ];

    public PromotionEditViewModel(PromotionApiService promotionService)
    {
        _promotionService = promotionService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("promotionId", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
        {
            _promotionId = id;
            _ = LoadExistingPromotion();
        }
    }

    private async Task LoadExistingPromotion()
    {
        IsBusy = true;
        try
        {
            var promos = await _promotionService.GetAllAsync();
            var promo = promos?.FirstOrDefault(p => p.Id == _promotionId);
            if (promo != null)
            {
                Name = promo.Name;
                DiscountType = promo.DiscountType;
                DiscountValue = promo.DiscountValue;
                ApplicableDay = promo.ApplicableDay;
                IsActive = promo.IsActive;

                IsPercent = DiscountType == DiscountType.Percent;
                IsFixed = DiscountType == DiscountType.Fixed;
                SelectedDayIndex = ApplicableDay.HasValue ? (int)ApplicableDay.Value + 1 : 0;
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

    partial void OnIsPercentChanged(bool value)
    {
        if (value) DiscountType = DiscountType.Percent;
    }

    partial void OnIsFixedChanged(bool value)
    {
        if (value) DiscountType = DiscountType.Fixed;
    }

    [RelayCommand]
    private void SetDiscountType(string type)
    {
        if (type == "Percent")
        {
            IsPercent = true;
            IsFixed = false;
            DiscountType = DiscountType.Percent;
        }
        else
        {
            IsPercent = false;
            IsFixed = true;
            DiscountType = DiscountType.Fixed;
        }
    }

    [RelayCommand]
    private void SelectDay(string dayIndex)
    {
        if (int.TryParse(dayIndex, out var idx))
        {
            if (idx == -1)
            {
                SelectedDayIndex = 0;
            }
            else if (idx == 7)
            {
                SelectedDayIndex = 1; // Sunday
            }
            else
            {
                SelectedDayIndex = idx + 1; // Monday (1) -> 2, Tuesday (2) -> 3, etc.
            }
        }
    }

    partial void OnSelectedDayIndexChanged(int value)
    {
        ApplicableDay = value <= 0 ? null : (DayOfWeek)(value - 1);
    }

    [RelayCommand]
    private async Task Save()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.EnterPromotionName, "OK");
            return;
        }
        if (DiscountValue <= 0)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.EnterDiscountSize, "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var promotion = new Promotion
            {
                Id = _promotionId,
                Name = Name,
                DiscountType = DiscountType,
                DiscountValue = DiscountValue,
                ApplicableDay = ApplicableDay,
                IsActive = IsActive
            };

            await _promotionService.SavePromotionAsync(promotion);
            await Shell.Current.GoToAsync("..");
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
}
