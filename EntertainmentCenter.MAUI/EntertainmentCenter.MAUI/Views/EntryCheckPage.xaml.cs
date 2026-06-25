using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

[QueryProperty(nameof(ScannedCode), "scannedCode")]
public partial class EntryCheckPage : ContentPage
{
    private readonly EntryCheckViewModel _vm;

    public EntryCheckPage(EntryCheckViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <summary>
    /// Set by Shell when navigating back from BarcodeScanPage with ?scannedCode=...
    /// </summary>
    public string ScannedCode
    {
        set
        {
            Console.WriteLine($"[EntryCheckPage] QueryProperty ScannedCode received: {value}");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Dispatcher.Dispatch(async () =>
                {
                    await _vm.ProcessScannedCodeAsync(Uri.UnescapeDataString(value));
                });
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadActiveSessionsCommand.ExecuteAsync(null);
    }
}
