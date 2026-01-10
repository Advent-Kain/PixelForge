using Avalonia.Controls;
using PixelForge.Editor.Services;

namespace PixelForge.Editor.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new AboutWindowViewModel();
    }

    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}

public class AboutWindowViewModel
{
    public string ProjectRoot => ProjectManager.ProjectRoot ?? "No project loaded";
}
