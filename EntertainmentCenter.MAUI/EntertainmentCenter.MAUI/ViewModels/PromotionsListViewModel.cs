using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Models;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class PromotionsListViewModel : ObservableObject
{
    private readonly PromotionApiService _promotionService;

    [ObservableProperty]
    private ObservableCollection<PromotionDisplay> promotions = [];

    [ObservableProperty]
    private bool isBusy;

    public PromotionsListViewModel(PromotionApiService promotionService)
    {
        _promotionService = promotionService;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var result = await _promotionService.GetAllAsync();
            Promotions = new ObservableCollection<PromotionDisplay>(
                (result ?? []).Select(p => new PromotionDisplay
                {
                    Id = p.Id,
                    Name = p.Name,
                    DiscountValue = p.DiscountValue,
                    DiscountType = p.DiscountType,
                    ApplicableDay = p.ApplicableDay,
                    IsActive = p.IsActive
                }));
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
    private async Task TogglePromotion(PromotionDisplay promo)
    {
        try
        {
            var p = new Promotion
            {
                Id = promo.Id,
                Name = promo.Name,
                DiscountValue = promo.DiscountValue,
                DiscountType = promo.DiscountType,
                ApplicableDay = promo.ApplicableDay,
                IsActive = !promo.IsActive
            };
            var saved = await _promotionService.SavePromotionAsync(p);
            if (saved != null)
                promo.IsActive = saved.IsActive;
        }
        catch (HttpRequestException)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.NoConnectionToServer, "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ServerError, "OK");
        }
    }

    [RelayCommand]
    private async Task EditPromotion(PromotionDisplay promo)
    {
        await Shell.Current.GoToAsync($"PromotionEditPage?promotionId={promo.Id}");
    }

    [RelayCommand]
    private async Task AddPromotion()
    {
        await Shell.Current.GoToAsync("PromotionEditPage");
    }

    [RelayCommand]
    private async Task DeletePromotion(PromotionDisplay promo)
    {
        bool confirm = await Shell.Current.DisplayAlert(
            AppResources.DeactivatePromotionTitle,
            string.Format(AppResources.DeactivatePromotionConfirm, promo.Name),
            AppResources.Yes, AppResources.CancelButton);

        if (!confirm) return;

        IsBusy = true;
        try
        {
            await _promotionService.DeletePromotionAsync(promo.Id);
            await LoadData();
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

public partial class PromotionDisplay : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal DiscountValue { get; set; }
    public DiscountType DiscountType { get; set; }
    public DayOfWeek? ApplicableDay { get; set; }
    public bool IsActive { get; set; } = true;

    public string DiscountLabel =>
        DiscountType == DiscountType.Percent ? $"-{DiscountValue}%" : $"-{DiscountValue} {AppResources.Lei}";

    public string DayLabel => ApplicableDay switch
    {
        DayOfWeek.Monday => AppResources.Mon,
        DayOfWeek.Tuesday => AppResources.Tue,
        DayOfWeek.Wednesday => AppResources.Wed,
        DayOfWeek.Thursday => AppResources.Thu,
        DayOfWeek.Friday => AppResources.Fri,
        DayOfWeek.Saturday => AppResources.Sat,
        DayOfWeek.Sunday => AppResources.Sun,
        _ => AppResources.AnyDay
    };
}
