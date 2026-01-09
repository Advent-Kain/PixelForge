using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Editor.Services;
using PixelForge.Shared.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for the main editor window.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

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
        InitializeProject(ProjectName);
        StatusText = "New project created";
    }

    [RelayCommand]
    private void OpenProject()
    {
        var projectFilePath = ResolveProjectFilePath();

        if (projectFilePath == null || !File.Exists(projectFilePath))
        {
            StatusText = "Project file not found";
            return;
        }

        var json = File.ReadAllText(projectFilePath);
        var projectFile = JsonSerializer.Deserialize<ProjectFile>(json);

        if (projectFile == null)
        {
            StatusText = "Failed to load project";
            return;
        }

        ProjectManager.SetProject(projectFile, projectFilePath);
        ProjectName = projectFile.Name;
        AddRecentProject(projectFilePath);

        var mapPath = ProjectManager.GetDefaultMapPath();
        if (mapPath != null && File.Exists(mapPath))
        {
            var mapJson = File.ReadAllText(mapPath);
            var mapData = JsonSerializer.Deserialize<MapData>(mapJson);
            if (mapData != null)
            {
                MapEditor = new MapEditorViewModel();
                MapEditor.LoadMap(mapData);
            }
        }

        StatusText = "Project opened";
    }

    [RelayCommand]
    private void SaveProject()
    {
        if (!ProjectManager.HasProject)
        {
            InitializeProject(ProjectName);
        }

        if (!ProjectManager.HasProject)
        {
            StatusText = "Unable to save project";
            return;
        }

        var projectFilePath = ProjectManager.ProjectFilePath!;
        var projectRoot = ProjectManager.ProjectRoot!;

        Directory.CreateDirectory(projectRoot);
        var mapsDirectory = ProjectManager.GetMapsDirectory();
        var databaseDirectory = ProjectManager.GetDatabaseDirectory();
        var assetsDirectory = ProjectManager.GetAssetsDirectory();

        if (mapsDirectory != null)
            Directory.CreateDirectory(mapsDirectory);
        if (databaseDirectory != null)
            Directory.CreateDirectory(databaseDirectory);
        if (assetsDirectory != null)
            Directory.CreateDirectory(assetsDirectory);

        var projectJson = JsonSerializer.Serialize(ProjectManager.CurrentProject, JsonOptions);
        File.WriteAllText(projectFilePath, projectJson);
        AddRecentProject(projectFilePath);

        if (MapEditor?.CurrentMap != null)
        {
            var mapPath = ProjectManager.GetDefaultMapPath();
            if (mapPath != null)
            {
                var mapJson = JsonSerializer.Serialize(MapEditor.CurrentMap, JsonOptions);
                File.WriteAllText(mapPath, mapJson);
            }
        }

        StatusText = "Project saved";
    }

    [RelayCommand]
    private void OpenDatabase()
    {
        // Open database editor window
        var window = new Views.DatabaseEditorWindow();
        window.Show();
        StatusText = "Opened database editor";
    }

    [RelayCommand]
    private void OpenScriptEditor()
    {
        // Open script editor window
        var window = new Views.ScriptEditorWindow();
        window.Show();
        StatusText = "Opened script editor";
    }

    [RelayCommand]
    private void OpenEventEditor()
    {
        if (MapEditor?.CurrentMap == null)
        {
            StatusText = "No map loaded";
            return;
        }

        var window = new Views.EventEditorWindow(MapEditor.CurrentMap);
        window.Show();
        StatusText = "Opened event editor";
    }

    [RelayCommand]
    private void OpenProjectSettings()
    {
        var window = new Views.ProjectSettingsWindow();
        window.Show();
        StatusText = "Opened project settings";
    }

    [RelayCommand]
    private void Exit()
    {
        // Application exit handled by window
    }

    private void InitializeProject(string projectName)
    {
        var projectRoot = GetDefaultProjectDirectory(projectName);
        var projectFilePath = Path.Combine(projectRoot, ProjectManager.ProjectFileName);
        var projectFile = ProjectManager.CreateDefaultProject(projectName);

        ProjectManager.SetProject(projectFile, projectFilePath);
        AddRecentProject(projectFilePath);
    }

    private string GetDefaultProjectDirectory(string projectName)
    {
        var sanitizedName = string.IsNullOrWhiteSpace(projectName) ? "New Project" : projectName.Trim();
        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PixelForgeProjects");
        return Path.Combine(baseDirectory, sanitizedName);
    }

    private string? ResolveProjectFilePath()
    {
        if (ProjectManager.ProjectFilePath != null)
            return ProjectManager.ProjectFilePath;

        if (RecentProjects.Count > 0)
            return RecentProjects[0];

        var defaultPath = Path.Combine(GetDefaultProjectDirectory(ProjectName), ProjectManager.ProjectFileName);
        return defaultPath;
    }

    private void AddRecentProject(string projectFilePath)
    {
        if (RecentProjects.Contains(projectFilePath))
            return;

        RecentProjects.Insert(0, projectFilePath);
    }
}
