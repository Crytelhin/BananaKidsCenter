using System;
using System.Threading.Tasks;

#if ANDROID
using Android.Gms.Extensions;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.CodeScanner;
#endif

namespace EntertainmentCenter.Services;

public class BarcodeScannerService : IBarcodeScannerService
{
    public async Task<string?> ScanAsync()
    {
#if ANDROID
        try
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null)
            {
                Console.WriteLine("[BarcodeScannerService] CurrentActivity is null");
                return null;
            }

            var options = new GmsBarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatQrCode,
                                   Barcode.FormatEan13,
                                   Barcode.FormatEan8,
                                   Barcode.FormatCode128,
                                   Barcode.FormatCode39)
                .Build();

            var scanner = GmsBarcodeScanning.GetClient(activity, options);
            var tcs = new TaskCompletionSource<string?>();

            var task = scanner.StartScan();

            task.AddOnSuccessListener(new OnSuccessListener(result =>
            {
                if (result is Barcode barcode)
                {
                    Console.WriteLine($"[BarcodeScannerService] Scan success: {barcode.RawValue}");
                    tcs.TrySetResult(barcode.RawValue);
                }
                else
                {
                    Console.WriteLine("[BarcodeScannerService] Scan success but result is not a Barcode");
                    tcs.TrySetResult(null);
                }
            }));

            task.AddOnFailureListener(new OnFailureListener(ex =>
            {
                Console.WriteLine($"[BarcodeScannerService] Scan failure: {ex.Message}");
                tcs.TrySetException(ex);
            }));

            task.AddOnCanceledListener(new OnCanceledListener(() =>
            {
                Console.WriteLine("[BarcodeScannerService] Scan canceled");
                tcs.TrySetResult(null);
            }));

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BarcodeScannerService] Exception during scan: {ex.Message}");
            return null;
        }
#else
        return await Task.FromResult<string?>(null);
#endif
    }
}

#if ANDROID
public class OnSuccessListener : Java.Lang.Object, Android.Gms.Tasks.IOnSuccessListener
{
    private readonly Action<Java.Lang.Object?> _action;

    public OnSuccessListener(Action<Java.Lang.Object?> action)
    {
        _action = action;
    }

    public void OnSuccess(Java.Lang.Object? result)
    {
        _action(result);
    }
}

public class OnFailureListener : Java.Lang.Object, Android.Gms.Tasks.IOnFailureListener
{
    private readonly Action<Java.Lang.Exception> _action;

    public OnFailureListener(Action<Java.Lang.Exception> action)
    {
        _action = action;
    }

    public void OnFailure(Java.Lang.Exception e)
    {
        _action(e);
    }
}

public class OnCanceledListener : Java.Lang.Object, Android.Gms.Tasks.IOnCanceledListener
{
    private readonly Action _action;

    public OnCanceledListener(Action action)
    {
        _action = action;
    }

    public void OnCanceled()
    {
        _action();
    }
}
#endif
