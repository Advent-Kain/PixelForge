using Avalonia.Controls;
using Avalonia.Interactivity;
using PixelForge.Editor.ViewModels;

namespace PixelForge.Editor.Views;

public partial class ExportWindow : Window
{
    public ExportWindow()
    {
        InitializeComponent();
        DataContext = new ExportViewModel();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
