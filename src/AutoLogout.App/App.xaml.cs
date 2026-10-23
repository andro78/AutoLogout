using System.Windows;
namespace AutoLogout.App;
public partial class App : System.Windows.Application
{
    private System.Threading.Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        instance = new System.Threading.Mutex(true, @"Local\AutoLogout.App", out var first);
        if (!first) { Shutdown(); return; }
        base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e)
    {
        instance?.Dispose();
        base.OnExit(e);
    }
}

