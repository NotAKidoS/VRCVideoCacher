using Avalonia.Controls;
using Avalonia.Threading;
using VRCVideoCacher.Utils;
using VRCVideoCacher.ViewModels;

namespace VRCVideoCacher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnWindowOpened;
        Dispatcher.UIThread.UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception != null && e.Exception is Exception ex)
        {
            LoggerUtils.LogUnhandledException(ex, "Unhandled UI thread exception");
        }
    }

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        // Only run once
        Opened -= OnWindowOpened;

        // Delay slightly to let the main window fully render
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(500);
            Program.InitializeUIBackend();
        });

        // On first launch let the user pick which games to patch and what to cache
        if (ConfigManager.IsFirstRunSetupPending)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(500);
                await ShowFirstRunSetupDialog();
            });
        }

        // Check if we should show the cookie setup wizard
        // Show if: cookies are enabled, setup not completed, and cookies not already valid
        if (ConfigManager.Config.YtdlpUseCookies &&
            !ConfigManager.Config.CookieSetupCompleted &&
            !Program.IsCookiesEnabledAndValid())
        {
            // Delay slightly to let the main window fully render
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(500);
                await ShowCookieSetupDialog();
            });
        }
    }

    private async Task ShowFirstRunSetupDialog()
    {
        var viewModel = new FirstRunSetupViewModel();
        var window = new FirstRunSetupWindow
        {
            DataContext = viewModel
        };

        viewModel.RequestClose += () => window.Close();

        await window.ShowDialog(this);
    }

    private async Task ShowCookieSetupDialog()
    {
        var viewModel = new CookieSetupViewModel();
        var window = new CookieSetupWindow
        {
            DataContext = viewModel
        };

        viewModel.RequestClose += () => window.Close();

        await window.ShowDialog(this);
    }
}
