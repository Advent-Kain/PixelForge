using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Editor.Services;

namespace PixelForge.Editor.ViewModels;

public partial class PluginManagerViewModel : ObservableObject
{
    private const string PluginConfigFileName = "plugins.json";
    private static readonly string[] PluginExtensions = { ".dll", ".js" };

    private readonly string _projectRoot;
    private readonly string _pluginFolder;
    private readonly string _configPath;

    [ObservableProperty]
    private ObservableCollection<PluginEntryViewModel> _plugins = new();

    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand OpenFolderCommand { get; }

    public PluginManagerViewModel()
    {
        _projectRoot = ProjectManager.ProjectRoot ?? Environment.CurrentDirectory;
        _pluginFolder = Path.Combine(_projectRoot, "Plugins");
        _configPath = Path.Combine(_projectRoot, PluginConfigFileName);

        RefreshCommand = new RelayCommand(LoadPlugins);
        SaveCommand = new RelayCommand(SavePlugins);
        OpenFolderCommand = new RelayCommand(OpenPluginFolder);

        LoadPlugins();
    }

    private void LoadPlugins()
    {
        Directory.CreateDirectory(_pluginFolder);
        Plugins.Clear();

        var config = LoadPluginConfig();
        var configLookup = config.ToDictionary(entry => entry.RelativePath, entry => entry);

        var pluginFiles = Directory.GetFiles(_pluginFolder, "*.*", SearchOption.AllDirectories)
            .Where(file => PluginExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName);

        foreach (var file in pluginFiles)
        {
            var relativePath = Path.GetRelativePath(_projectRoot, file);
            var entry = configLookup.TryGetValue(relativePath, out var state)
                ? state
                : new PluginEntry(relativePath, true);

            Plugins.Add(new PluginEntryViewModel
            {
                Name = Path.GetFileNameWithoutExtension(file),
                RelativePath = entry.RelativePath,
                IsEnabled = entry.IsEnabled
            });
        }
    }

    private void SavePlugins()
    {
        var entries = Plugins
            .Select(plugin => new PluginEntry(plugin.RelativePath, plugin.IsEnabled))
            .ToList();

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    private List<PluginEntry> LoadPluginConfig()
    {
        if (!File.Exists(_configPath))
        {
            return new List<PluginEntry>();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<List<PluginEntry>>(json) ?? new List<PluginEntry>();
        }
        catch
        {
            return new List<PluginEntry>();
        }
    }

    private void OpenPluginFolder()
    {
        if (!Directory.Exists(_pluginFolder))
        {
            Directory.CreateDirectory(_pluginFolder);
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _pluginFolder,
            UseShellExecute = true
        });
    }
}

public record PluginEntry(string RelativePath, bool IsEnabled);

public partial class PluginEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _relativePath = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;
}
