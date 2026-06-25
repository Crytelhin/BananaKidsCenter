using System.Threading.Tasks;

namespace EntertainmentCenter.Services;

public interface IBarcodeScannerService
{
    Task<string?> ScanAsync();
}
