using Avalonia.Controls;
using VRCVideoCacher.ViewModels;

namespace VRCVideoCacher.Views;

public partial class FirstRunSetupWindow : Window
{
    public FirstRunSetupWindow()
    {
        InitializeComponent();
        Closed += (_, _) => (DataContext as FirstRunSetupViewModel)?.OnWindowClosed();
    }
}
