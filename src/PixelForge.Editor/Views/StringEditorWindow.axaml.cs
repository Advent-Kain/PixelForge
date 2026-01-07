using Avalonia.Controls;
using PixelForge.Editor.ViewModels;

namespace PixelForge.Editor.Views;

public partial class StringEditorWindow : Window
{
    public StringEditorWindow()
    {
        InitializeComponent();
        DataContext = new StringEditorViewModel();
    }
}
