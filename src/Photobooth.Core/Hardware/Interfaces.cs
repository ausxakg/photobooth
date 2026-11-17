namespace Photobooth.Core.Hardware;

public enum CameraState { Disconnected, Connecting, Ready, Busy, Error }

public sealed record CameraInfo(string Id, string Name, string Provider);

public interface ICameraProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<CameraInfo>> DetectAsync(CancellationToken ct = default);
    Task ConnectAsync(CameraInfo camera, CancellationToken ct = default);
    Task DisconnectAsync();
    CameraState State { get; }
    event Action<CameraState>? StateChanged;
    /// <summary>Live preview frames (JPEG bytes).</summary>
    IAsyncEnumerable<byte[]> PreviewFramesAsync(CancellationToken ct = default);
    /// <summary>Captures a full-resolution still and writes it to destinationPath.</summary>
    Task<string> CaptureAsync(string destinationPath, CancellationToken ct = default);
}

public interface IPrintService
{
    Task<IReadOnlyList<string>> GetPrintersAsync();
    Task PrintAsync(string printerName, string imagePath, int copies, CancellationToken ct = default);
}
