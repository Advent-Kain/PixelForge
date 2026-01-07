using Avalonia.Controls;
using PixelForge.Editor.ViewModels;

namespace PixelForge.Editor.Views;

public partial class DatabaseEditorWindow : Window
{
    public DatabaseEditorWindow()
    {
        InitializeComponent();
        DataContext = new DatabaseEditorViewModel();
    }
}
