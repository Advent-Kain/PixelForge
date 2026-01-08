using Avalonia.Controls;
using PixelForge.Editor.ViewModels;
using PixelForge.Shared.Models;

namespace PixelForge.Editor.Views;

public partial class EventEditorWindow : Window
{
    public EventEditorWindow()
        : this(null)
    {
    }

    public EventEditorWindow(MapData? map)
    {
        InitializeComponent();
        DataContext = new EventEditorViewModel(map);
    }
}
