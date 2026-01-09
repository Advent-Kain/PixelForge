using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Editor.Services;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for project settings.
/// </summary>
public partial class ProjectSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _runtimeProjectPath;

    [ObservableProperty]
    private string _gameTitle = "PixelForge Game";

    [ObservableProperty]
    private int _windowWidth = 1280;

    [ObservableProperty]
    private int _windowHeight = 720;

    [ObservableProperty]
    private string _selectedBattleMode = "TurnBased";

    public ObservableCollection<string> BattleModes { get; } = new()
    {
        "TurnBased",
        "ATB",
        "ActionEconomy"
    };

    public ICommand SaveCommand { get; }

    public ProjectSettingsViewModel()
    {
        LoadFromProject();
        SaveCommand = new RelayCommand(SaveToProject);
    }

    private void LoadFromProject()
    {
        var project = ProjectManager.CurrentProject;
        if (project == null)
            return;

        RuntimeProjectPath = project.RuntimeProjectPath;
        GameTitle = project.GameSettings.Title;
        WindowWidth = project.GameSettings.WindowWidth;
        WindowHeight = project.GameSettings.WindowHeight;
        SelectedBattleMode = project.GameSettings.BattleMode;
    }

    private void SaveToProject()
    {
        var project = ProjectManager.CurrentProject;
        if (project == null)
            return;

        project.RuntimeProjectPath = string.IsNullOrWhiteSpace(RuntimeProjectPath)
            ? null
            : RuntimeProjectPath.Trim();
        project.GameSettings.Title = GameTitle;
        project.GameSettings.WindowWidth = WindowWidth;
        project.GameSettings.WindowHeight = WindowHeight;
        project.GameSettings.BattleMode = SelectedBattleMode;
    }
}
