using Avalonia.Controls;
using PixelForge.Editor.ViewModels;

namespace PixelForge.Editor.Views;

public partial class PluginManagerWindow : Window
{
    public PluginManagerWindow()
    {
        InitializeComponent();
        DataContext = new PluginManagerViewModel();
    }
}
