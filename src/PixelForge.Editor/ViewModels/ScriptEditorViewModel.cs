using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for the C# script editor.
/// </summary>
public partial class ScriptEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    private ScriptFile? _selectedScript;

    [ObservableProperty]
    private string _scriptContent = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ObservableCollection<ScriptFile> Scripts { get; } = new();

    private string _scriptsDirectory = "Scripts";

    public ScriptEditorViewModel()
    {
        LoadScripts();
    }

    /// <summary>
    /// Load all scripts from directory.
    /// </summary>
    private void LoadScripts()
    {
        if (!Directory.Exists(_scriptsDirectory))
        {
            Directory.CreateDirectory(_scriptsDirectory);
            CreateSampleScript();
        }

        Scripts.Clear();
        foreach (var file in Directory.GetFiles(_scriptsDirectory, "*.cs"))
        {
            var scriptFile = new ScriptFile
            {
                Name = Path.GetFileName(file),
                Path = file
            };
            Scripts.Add(scriptFile);
        }

        SelectedScript = Scripts.FirstOrDefault();
        if (SelectedScript != null)
        {
            LoadScript(SelectedScript);
        }
    }

    /// <summary>
    /// Create a sample script for demonstration.
    /// </summary>
    private void CreateSampleScript()
    {
        string sampleScript = @"using PixelForge.Engine.Core;
using PixelForge.Engine.Scripting;

namespace GameScripts;

/// <summary>
/// Sample custom script demonstrating the scripting API.
/// </summary>
public class SampleScript : GameScript
{
    private float timeOfDay = 12f;

    public override void OnGameStart()
    {
        // Called when the game starts
        Game.ShowMessage(""Welcome to PixelForge!"");
    }

    public override void OnMapLoad(Map map)
    {
        // Called when a map loads
        timeOfDay = 12f;
        UpdateLighting();
    }

    public void Update(float deltaTime)
    {
        // Update time of day
        timeOfDay += deltaTime / 60f;
        if (timeOfDay >= 24f)
            timeOfDay -= 24f;

        UpdateLighting();
    }

    private void UpdateLighting()
    {
        if (timeOfDay >= 6f && timeOfDay < 18f)
        {
            // Daytime - normal lighting
            Game.SetScreenTint(255, 255, 255, 255);
        }
        else
        {
            // Nighttime - blue tint
            Game.SetScreenTint(100, 100, 150, 200);
        }
    }
}
";

        string path = Path.Combine(_scriptsDirectory, "SampleScript.cs");
        File.WriteAllText(path, sampleScript);
    }

    /// <summary>
    /// Load script content.
    /// </summary>
    private void LoadScript(ScriptFile script)
    {
        try
        {
            ScriptContent = File.ReadAllText(script.Path);
            StatusMessage = $"Loaded: {script.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading script: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NewScript()
    {
        string template = @"using PixelForge.Engine.Core;
using PixelForge.Engine.Scripting;

namespace GameScripts;

/// <summary>
/// Custom game script.
/// </summary>
public class NewScript : GameScript
{
    public override void OnGameStart()
    {
        // Game start logic
    }

    public override void OnMapLoad(Map map)
    {
        // Map load logic
    }
}
";

        int count = Scripts.Count(s => s.Name.StartsWith("NewScript"));
        string fileName = count == 0 ? "NewScript.cs" : $"NewScript{count + 1}.cs";
        string path = Path.Combine(_scriptsDirectory, fileName);

        File.WriteAllText(path, template);

        var scriptFile = new ScriptFile
        {
            Name = fileName,
            Path = path
        };
        Scripts.Add(scriptFile);
        SelectedScript = scriptFile;
        LoadScript(scriptFile);

        StatusMessage = $"Created: {fileName}";
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedScript == null)
        {
            StatusMessage = "No script selected";
            return;
        }

        try
        {
            File.WriteAllText(SelectedScript.Path, ScriptContent);
            StatusMessage = $"Saved: {SelectedScript.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedScript == null)
            return;

        try
        {
            File.Delete(SelectedScript.Path);
            Scripts.Remove(SelectedScript);
            SelectedScript = Scripts.FirstOrDefault();
            if (SelectedScript != null)
                LoadScript(SelectedScript);
            StatusMessage = "Script deleted";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Compile()
    {
        // TODO: Compile scripts using Roslyn
        StatusMessage = "Compilation not yet implemented";
    }

    partial void OnSelectedScriptChanged(ScriptFile? value)
    {
        if (value != null)
        {
            LoadScript(value);
        }
    }
}

/// <summary>
/// Represents a script file.
/// </summary>
public class ScriptFile
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}
