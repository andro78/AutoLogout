using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace AutoLogout.App;

internal static class BrowserLauncher
{
    // Read profile metadata only. Browser databases are never changed directly.
    internal static Dictionary<string, int> StartRegisteredBrowsers(List<string> notes, string requestId)
    {
        var targets = new Dictionary<string, int>();
        foreach (var (browser, vendor, executable) in new[] {
            ("chrome", @"Google\Chrome", "chrome.exe"),
            ("edge", @"Microsoft\Edge", "msedge.exe") })
        {
            try
            {
                using var host = Registry.CurrentUser.OpenSubKey($@"Software\{vendor}\NativeMessagingHosts\com.autologout.bridge");
                if (host?.GetValue(null) is not string manifest || !File.Exists(manifest)) continue;
                using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
                var ids = doc.RootElement.GetProperty("allowed_origins").EnumerateArray()
                    .Select(x => new Uri(x.GetString()!).Host).ToArray();
                var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), vendor, "User Data");
                if (!Directory.Exists(root)) continue;
                var profiles = Directory.GetDirectories(root).Where(path =>
                    HasExtension(path, ids)).ToArray();
                if (profiles.Length == 0) { notes.Add($"{browser}: 확장이 설치된 프로필을 찾지 못함"); continue; }
                targets[browser] = profiles.Length;
                var connected = new HashSet<string>();
                var statusFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoLogout");
                if (Directory.Exists(statusFolder)) foreach (var file in Directory.GetFiles(statusFolder, browser + "-*.json"))
                {
                    try
                    {
                        using var state = JsonDocument.Parse(File.ReadAllText(file));
                        if (DateTime.UtcNow - state.RootElement.GetProperty("seenUtc").GetDateTime() < TimeSpan.FromSeconds(20))
                            connected.Add(state.RootElement.GetProperty("profile").GetString()!);
                    }
                    catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException or InvalidOperationException) { }
                }
                if (connected.Count >= profiles.Length) { notes.Add($"{browser}: 연결된 프로필에 정리 요청"); continue; }
                // Open the specific profiles, including when another profile is already running.
                var exe = FindExecutable(executable, vendor);
                if (exe == null) { notes.Add($"{browser}: 실행 파일을 찾지 못함"); continue; }
                foreach (var profile in profiles)
                {
                    var start = new ProcessStartInfo(exe) { UseShellExecute = false };
                    start.ArgumentList.Add("--profile-directory=" + Path.GetFileName(profile));
                    start.ArgumentList.Add("--new-window");
                    start.ArgumentList.Add("about:blank#autologout-" + requestId);
                    Process.Start(start)?.Dispose();
                }
                notes.Add($"{browser}: {profiles.Length}개 프로필에 정리 요청");
            }
            catch (Exception ex) { notes.Add($"{browser}: {ex.Message}"); }
        }
        return targets;
    }

    internal static bool HasExtension(string profile, string[] ids)
    {
        foreach (var name in new[] { "Preferences", "Secure Preferences" })
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(profile, name)));
                if (doc.RootElement.TryGetProperty("extensions", out var extensions) &&
                    extensions.TryGetProperty("settings", out var settings) &&
                    ids.Any(id => settings.TryGetProperty(id, out _))) return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        }
        return false;
    }

    private static string? FindExecutable(string executable, string vendor)
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var key = hive.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\App Paths\{executable}");
            if (key?.GetValue(null) is string path && File.Exists(path.Trim('"'))) return path.Trim('"');
        }
        foreach (var folder in new[] { Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
        {
            var path = Path.Combine(Environment.GetFolderPath(folder), vendor, "Application", executable);
            if (File.Exists(path)) return path;
        }
        return null;
    }
}
