using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PixelForge.Editor.Services;
using PixelForge.Shared.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

    private readonly IStorageProvider _storageProvider;
    private TestPlayService? _testPlayService;

    [ObservableProperty]
    private bool _isTestPlayRunning;

    [ObservableProperty]
    private string _testPlayLabel = "Test Play";

    public MainWindowViewModel(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
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
    private async System.Threading.Tasks.Task OpenProjectAsync()
    {
        var projectFilePath = await PickProjectFileAsync();
        if (string.IsNullOrWhiteSpace(projectFilePath))
            return;

        LoadProjectFromFile(projectFilePath);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SaveProjectAsync()
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

        var projectFilePath = ProjectManager.ProjectFilePath ?? await PickSaveProjectFileAsync();
        if (string.IsNullOrWhiteSpace(projectFilePath))
        {
            StatusText = "Save canceled";
            return;
        }

        var projectRoot = Path.GetDirectoryName(projectFilePath);
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            StatusText = "Unable to resolve project folder";
            return;
        }

        ProjectManager.SetProject(ProjectManager.CurrentProject!, projectFilePath);

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
        ShowWindow(window);
        StatusText = "Opened database editor";
    }

    [RelayCommand]
    private void OpenAssetBrowser()
    {
        var window = new Views.AssetBrowserWindow();
        ShowWindow(window);
        StatusText = "Opened asset browser";
    }

    [RelayCommand]
    private void OpenDialogueEditor()
    {
        var window = new Views.DialogueEditorWindow();
        ShowWindow(window);
        StatusText = "Opened dialogue editor";
    }

    [RelayCommand]
    private void OpenStringEditor()
    {
        var window = new Views.StringEditorWindow();
        ShowWindow(window);
        StatusText = "Opened string editor";
    }

    [RelayCommand]
    private void OpenScriptEditor()
    {
        // Open script editor window
        var window = new Views.ScriptEditorWindow();
        ShowWindow(window);
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
        ShowWindow(window);
        StatusText = "Opened event editor";
    }

    [RelayCommand]
    private void OpenProjectSettings()
    {
        var window = new Views.ProjectSettingsWindow();
        ShowWindow(window);
        StatusText = "Opened project settings";
    }

    [RelayCommand]
    private void OpenExport()
    {
        var window = new Views.ExportWindow();
        ShowWindow(window);
        StatusText = "Opened export window";
    }

    [RelayCommand]
    private void OpenPluginManager()
    {
        var window = new Views.PluginManagerWindow();
        ShowWindow(window);
        StatusText = "Opened plugin manager";
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task TestPlayAsync()
    {
        if (!EnsureProjectAvailable())
            return;

        if (_testPlayService == null)
        {
            _testPlayService = new TestPlayService(ProjectManager.ProjectRoot!);
            _testPlayService.GameStarted += (_, _) => SetTestPlayState(true);
            _testPlayService.GameStopped += (_, _) => SetTestPlayState(false);
            _testPlayService.OutputReceived += (_, message) => StatusText = message;
        }

        if (_testPlayService.IsRunning)
        {
            _testPlayService.StopTestPlay();
            StatusText = "Test play stopped";
            return;
        }

        StatusText = "Starting test play...";
        var started = await _testPlayService.StartTestPlayAsync();
        if (!started)
        {
            StatusText = "Failed to start test play";
        }
    }

    [RelayCommand]
    private void OpenDocumentation()
    {
        var root = ProjectManager.ProjectRoot ?? Environment.CurrentDirectory;
        var readmePath = Path.Combine(root, "README.md");
        OpenPathWithShell(readmePath);
    }

    [RelayCommand]
    private void OpenAbout()
    {
        var window = new Views.AboutWindow();
        ShowWindow(window);
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private void OpenRecentProject(string? projectFilePath)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath))
            return;

        LoadProjectFromFile(projectFilePath);
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

    private void AddRecentProject(string projectFilePath)
    {
        if (RecentProjects.Contains(projectFilePath))
            return;

        RecentProjects.Insert(0, projectFilePath);
    }

    private bool EnsureProjectAvailable()
    {
        if (ProjectManager.HasProject)
            return true;

        StatusText = "Save or open a project before test play.";
        return false;
    }

    private void SetTestPlayState(bool isRunning)
    {
        IsTestPlayRunning = isRunning;
        TestPlayLabel = isRunning ? "Stop" : "Test Play";
    }

    private void LoadProjectFromFile(string projectFilePath)
    {
        if (!File.Exists(projectFilePath))
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

    private async System.Threading.Tasks.Task<string?> PickProjectFileAsync()
    {
        var results = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Open Project",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PixelForge Project")
                {
                    Patterns = new[] { ProjectManager.ProjectFileName, "*.json" }
                }
            }
        });

        var file = results.Count > 0 ? results[0] : null;
        return file?.TryGetLocalPath();
    }

    private async System.Threading.Tasks.Task<string?> PickSaveProjectFileAsync()
    {
        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Project",
            SuggestedFileName = ProjectManager.ProjectFileName
        });

        return file?.TryGetLocalPath();
    }

    private void OpenPathWithShell(string path)
    {
        try
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            else
            {
                StatusText = "Documentation not found.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to open documentation: {ex.Message}";
        }
    }

    private void ShowWindow(Window window)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is { } mainWindow && window != mainWindow)
        {
            window.Show(mainWindow);
            return;
        }

        window.Show();
    }
}
