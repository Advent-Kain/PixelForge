using Avalonia.Controls;
using Avalonia.Interactivity;
using PixelForge.Editor.ViewModels;

namespace PixelForge.Editor.Views;

public partial class ProjectSettingsWindow : Window
{
    public ProjectSettingsWindow()
    {
        InitializeComponent();
        DataContext = new ProjectSettingsViewModel();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
