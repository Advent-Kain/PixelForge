using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Shared.Models;
using System.Collections.ObjectModel;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for the main editor window.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _projectName = "New Project";

    [ObservableProperty]
    private MapEditorViewModel? _mapEditor;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<string> RecentProjects { get; } = new();

    public MainWindowViewModel()
    {
        // Create default map editor
        MapEditor = new MapEditorViewModel();
    }

    [RelayCommand]
    private void NewProject()
    {
        ProjectName = "New Project";
        MapEditor = new MapEditorViewModel();
        StatusText = "New project created";
    }

    [RelayCommand]
    private void OpenProject()
    {
        StatusText = "Open project not yet implemented";
        // TODO: Implement project opening
    }

    [RelayCommand]
    private void SaveProject()
    {
        StatusText = "Save project not yet implemented";
        // TODO: Implement project saving
    }

    [RelayCommand]
    private void Exit()
    {
        // Application exit handled by window
    }
}
