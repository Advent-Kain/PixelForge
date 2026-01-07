using Avalonia.Controls;
using PixelForge.Editor.ViewModels;

namespace PixelForge.Editor.Views;

public partial class ScriptEditorWindow : Window
{
    public ScriptEditorWindow()
    {
        InitializeComponent();
        DataContext = new ScriptEditorViewModel();
    }
}
