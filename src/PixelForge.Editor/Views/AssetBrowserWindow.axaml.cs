using Avalonia.Controls;
using PixelForge.Editor.ViewModels;

namespace PixelForge.Editor.Views;

public partial class AssetBrowserWindow : Window
{
    public AssetBrowserWindow()
    {
        InitializeComponent();
        DataContext = new AssetBrowserViewModel();
    }
}
