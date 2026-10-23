using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
// Test harnesses can isolate protocol I/O without touching user settings.
var folder = Environment.GetEnvironmentVariable("AUTOLOGOUT_DATA_DIR")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoLogout");
Directory.CreateDirectory(folder);
using var input = Console.OpenStandardInput();
using var output = Console.OpenStandardOutput();
using var stop = new CancellationTokenSource();
var session = Guid.NewGuid().ToString("N");
var reader = Task.Run(async () =>
{
    try
    {
        var header = new byte[4];
        while (!stop.IsCancellationRequested)
        {
            await input.ReadExactlyAsync(header, stop.Token);
            var length = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (length < 1 || length > 65536) break;
            var bytes = new byte[length];
            await input.ReadExactlyAsync(bytes, stop.Token);
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;
            var browser = root.GetProperty("browser").GetString();
            if (browser is not ("chrome" or "edge")) continue;
            var state = JsonSerializer.Serialize(new {
                seenUtc = DateTime.UtcNow, session,
                profile = root.GetProperty("profile").GetString(), message = root.Clone()
            });
            var path = Path.Combine(folder, browser + "-" + session + ".json");
            File.WriteAllText(path + ".tmp", state);
            File.Move(path + ".tmp", path, true);
        }
    }
    catch (Exception ex) when (ex is IOException or OperationCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
    { Console.Error.WriteLine(ex.Message); }
    finally { stop.Cancel(); }
});
try
{
    while (!stop.IsCancellationRequested)
    {
        try
        {
            using var settings = JsonDocument.Parse(File.Exists(Path.Combine(folder, "settings.json")) ? File.ReadAllText(Path.Combine(folder, "settings.json")) : "{}");
            using var signal = JsonDocument.Parse(File.Exists(Path.Combine(folder, "signal.json")) ? File.ReadAllText(Path.Combine(folder, "signal.json")) : "{}");
            // Replay pending requests; extensions acknowledge only successful cleanup.
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type = "state", settings = settings.RootElement, signal = signal.RootElement }));
            if (bytes.Length > 900000) throw new InvalidOperationException("Settings too large");
            var header = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
            await output.WriteAsync(header, stop.Token);
            await output.WriteAsync(bytes, stop.Token);
            await output.FlushAsync(stop.Token);
        }
        catch (JsonException) { }
        await Task.Delay(5000, stop.Token);
    }
}
catch (Exception ex) when (ex is IOException or OperationCanceledException or InvalidOperationException)
{ Console.Error.WriteLine(ex.Message); }
finally { stop.Cancel(); }

