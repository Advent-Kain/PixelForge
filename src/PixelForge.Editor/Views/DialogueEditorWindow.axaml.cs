using Avalonia.Controls;
using PixelForge.Editor.ViewModels;

namespace PixelForge.Editor.Views;

public partial class DialogueEditorWindow : Window
{
    public DialogueEditorWindow()
    {
        InitializeComponent();
        DataContext = new DialogueEditorViewModel();
    }
}
