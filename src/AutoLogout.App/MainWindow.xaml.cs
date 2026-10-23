using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
namespace AutoLogout.App;
public partial class MainWindow : Window
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoLogout");
    private static readonly string SettingsFile = Path.Combine(Folder, "settings.json");
    private static readonly string SignalFile = Path.Combine(Folder, "signal.json");
    private const int WM_QUERYENDSESSION = 0x0011;
    private const int WM_ENDSESSION = 0x0016;
    private bool signaled;
    private bool quitting;
    private readonly System.Windows.Forms.NotifyIcon tray = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly System.Windows.Controls.TextBlock connections = new() { TextWrapping = TextWrapping.Wrap };
    private void ShowSettings() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void UpdateConnections()
    {
        var lines = new List<string>();
        foreach (var browser in new[] { "chrome", "edge" })
        {
            int count = 0;
            foreach (var path in Directory.GetFiles(Folder, browser + "-*.json"))
            {
                try {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    if (DateTime.UtcNow - doc.RootElement.GetProperty("seenUtc").GetDateTime() < TimeSpan.FromSeconds(20)) count++;
                } catch { }
            }
            lines.Add(browser + ": " + (count > 0 ? $"연결됨 ({count}개 호스트)" : "연결 확인 안 됨"));
        }
        connections.Text = string.Join(" / ", lines);
    }
    private Settings settings = new();
    public MainWindow()
    {
        InitializeComponent();
        ((System.Windows.Controls.StackPanel)Status.Parent).Children.Insert(1, connections);
        tray.Icon = System.Drawing.SystemIcons.Application;
        tray.Text = "AutoLogout v0.2";
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("설정 열기", null, (_, _) => Dispatcher.Invoke(ShowSettings));
        menu.Items.Add("지금 정리", null, (_, _) => Dispatcher.Invoke(() => RunNow_Click(this, new RoutedEventArgs())));
        menu.Items.Add("프로그램 종료", null, (_, _) => Dispatcher.Invoke(() => { quitting = true; Close(); }));
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowSettings);
        tray.Visible = true;
        Closing += (_, e) => { if (!quitting && !signaled) { e.Cancel = true; Hide(); } };
        Closed += (_, _) => { timer.Stop(); tray.Dispose(); };
        timer.Tick += (_, _) => UpdateConnections();
        Directory.CreateDirectory(Folder);
        timer.Start();
        try { if (File.Exists(SettingsFile)) settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsFile)) ?? new(); }
        catch (Exception ex) { Status.Text = "설정 읽기 실패: " + ex.Message; }
        Sites.Text = string.Join(Environment.NewLine, settings.Origins);
        AtStartup.IsChecked = settings.OnStartup;
        AtShutdown.IsChecked = settings.OnShutdown;
        Autostart.IsChecked = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")?.GetValue("AutoLogout") != null;
        SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
        Loaded += (_, _) => {
            if (Environment.GetCommandLineArgs().Contains("--startup") && settings.OnStartup)
                Signal("windows-startup");
            if (Environment.GetCommandLineArgs().Contains("--startup")) { Hide(); }
            if (string.IsNullOrEmpty(Status.Text)) Status.Text = "확장 프로그램 설치 후 Chrome/Edge에서 브라우저를 다시 실행하거나 '지금 정리'를 누르세요.";
        };
    }
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled)
    {
        if (msg == WM_QUERYENDSESSION && settings.OnShutdown && !signaled)
        {
            try { Signal("windows-ending"); signaled = true; } catch { }
        }
        if (msg == WM_ENDSESSION && wp == IntPtr.Zero) signaled = false;
        return IntPtr.Zero;
    }
    private string[] ReadOrigins()
    {
        var lines = Sites.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var origin in lines)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var u) || u.Scheme != "https" || u.AbsolutePath != "/" || u.Query.Length > 0 || u.Fragment.Length > 0 || u.UserInfo.Length > 0)
                throw new InvalidOperationException("올바른 HTTPS Origin만 입력하세요: " + origin);
        }
        return lines;
    }
    private void Persist()
    {
        settings = new Settings { Origins = ReadOrigins(), OnStartup = AtStartup.IsChecked == true, OnShutdown = AtShutdown.IsChecked == true };
        AtomicWrite(SettingsFile, JsonSerializer.Serialize(settings));
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (Autostart.IsChecked == true)
            key.SetValue("AutoLogout", $"\"{Environment.ProcessPath}\" --startup");
        else key.DeleteValue("AutoLogout", false);
    }
    private static void AtomicWrite(string path, string content)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, content);
        File.Move(temporary, path, true);
    }
    private void Signal(string reason)
    {
        AtomicWrite(SignalFile, JsonSerializer.Serialize(new SignalInfo { Id = Guid.NewGuid().ToString("N"), Reason = reason, CreatedUtc = DateTime.UtcNow }));
        Status.Text = $"요청 전송: {reason} ({DateTime.Now:HH:mm:ss}). 브라우저 확장에서 완료 여부를 확인하세요.";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { Persist(); Status.Text = "설정 저장 완료"; }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "설정 오류", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void RunNow_Click(object sender, RoutedEventArgs e)
    {
        try { Persist(); if (settings.Origins.Length == 0) throw new InvalidOperationException("사이트를 먼저 추가하세요."); Signal("manual"); }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "실행 오류", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
public sealed class Settings { public string[] Origins { get; set; } = []; public bool OnStartup { get; set; } = true; public bool OnShutdown { get; set; } = true; }
public sealed class SignalInfo { public string Id { get; set; } = ""; public string Reason { get; set; } = ""; public DateTime CreatedUtc { get; set; } }

