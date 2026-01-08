using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PixelForge.Engine.Scripting;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    public ObservableCollection<ScriptDiagnostic> Diagnostics { get; } = new();

    private string _scriptsDirectory = "Scripts";
    private readonly string _compiledOutputDirectory;

    [ObservableProperty]
    private bool _hasDiagnostics;

    public ScriptEditorViewModel()
    {
        _compiledOutputDirectory = Path.Combine(_scriptsDirectory, "Compiled");
        Diagnostics.CollectionChanged += OnDiagnosticsChanged;
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
        Diagnostics.Clear();

        if (!Directory.Exists(_scriptsDirectory))
        {
            StatusMessage = "No scripts directory found";
            return;
        }

        var scriptFiles = Directory.GetFiles(_scriptsDirectory, "*.cs");
        if (scriptFiles.Length == 0)
        {
            StatusMessage = "No scripts found to compile";
            return;
        }

        var syntaxTrees = scriptFiles
            .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file))
            .ToList();

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(location => MetadataReference.CreateFromFile(location));

        var compilation = CSharpCompilation.Create(
            assemblyName: "PixelForge.Scripts",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable)
        );

        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream, pdbStream);

        foreach (var diagnostic in emitResult.Diagnostics.OrderBy(d => d.Location.SourceTree?.FilePath))
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            var linePosition = lineSpan.StartLinePosition;
            Diagnostics.Add(new ScriptDiagnostic
            {
                Severity = diagnostic.Severity.ToString(),
                Id = diagnostic.Id,
                Message = diagnostic.GetMessage(),
                File = string.IsNullOrWhiteSpace(lineSpan.Path) ? "Unknown" : Path.GetFileName(lineSpan.Path),
                Line = linePosition.Line + 1,
                Column = linePosition.Character + 1
            });
        }

        var errorCount = emitResult.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        var warningCount = emitResult.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

        if (errorCount > 0)
        {
            StatusMessage = $"Compilation failed: {errorCount} error(s), {warningCount} warning(s)";
            return;
        }

        Directory.CreateDirectory(_compiledOutputDirectory);
        var assemblyPath = Path.Combine(_compiledOutputDirectory, "PixelForge.Scripts.dll");
        var pdbPath = Path.Combine(_compiledOutputDirectory, "PixelForge.Scripts.pdb");

        File.WriteAllBytes(assemblyPath, peStream.ToArray());
        File.WriteAllBytes(pdbPath, pdbStream.ToArray());

        ScriptRuntime.Instance.Clear();
        ScriptRuntime.Instance.LoadAssembly(peStream.ToArray(), pdbStream.ToArray());
        StatusMessage = warningCount > 0
            ? $"Compilation succeeded with {warningCount} warning(s)"
            : "Compilation succeeded";
    }

    partial void OnSelectedScriptChanged(ScriptFile? value)
    {
        if (value != null)
        {
            LoadScript(value);
        }
    }

    private void OnDiagnosticsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasDiagnostics = Diagnostics.Count > 0;
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

/// <summary>
/// Represents a script compilation diagnostic.
/// </summary>
public class ScriptDiagnostic
{
    public string Severity { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}
