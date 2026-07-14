using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRCVideoCacher.Utils;

namespace VRCVideoCacher.ViewModels;

public partial class FirstRunSetupViewModel : ViewModelBase
{
    public event Action? RequestClose;

    // Pre-select detected games, but nothing is applied until the user confirms.
    [ObservableProperty]
    private bool _patchVrChat = FileTools.IsVrChatInstalled;

    [ObservableProperty]
    private bool _patchResonite = FileTools.IsResoniteInstalled;

    [ObservableProperty]
    private bool _patchChilloutVR = FileTools.IsChilloutVRInstalled;

    [ObservableProperty]
    private bool _cacheYouTube = true;

    [ObservableProperty]
    private bool _cacheDanceVideos = true;

    [ObservableProperty]
    private bool _addVrcxAutoStart = true;

    public bool IsVrChatNotDetected => !FileTools.IsVrChatInstalled;
    public bool IsResoniteNotDetected => !FileTools.IsResoniteInstalled;
    public bool IsChilloutVRNotDetected => !FileTools.IsChilloutVRInstalled;
    public bool IsVrcxAutoStartVisible => OperatingSystem.IsWindows();

    private bool _applied;

    [RelayCommand]
    private void Continue()
    {
        Apply();
        RequestClose?.Invoke();
    }

    private void Apply()
    {
        if (_applied)
            return;
        _applied = true;

        var config = ConfigManager.Config;
        config.PatchVrChat = PatchVrChat;
        config.PatchResonite = PatchResonite;
        config.PatchChilloutVR = PatchChilloutVR;
        config.CacheYouTube = CacheYouTube;
        config.CachePyPyDance = CacheDanceVideos;
        config.CacheVrDancing = CacheDanceVideos;
        ConfigManager.IsFirstRunSetupPending = false;
        ConfigManager.TrySaveConfig();

        if (OperatingSystem.IsWindows() && AddVrcxAutoStart)
            AutoStartShortcut.CreateShortcut();

        Task.Run(FileTools.BackupAllYtdl);
    }

    // Closing the window without pressing Continue skips setup.
    public void OnWindowClosed()
    {
        if (_applied) return;
        ConfigManager.IsFirstRunSetupPending = false;
        ConfigManager.TrySaveConfig();
    }
}
