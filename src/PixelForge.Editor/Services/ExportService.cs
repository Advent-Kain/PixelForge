using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace PixelForge.Editor.Services;

/// <summary>
/// Service for exporting/packaging the game for distribution.
/// </summary>
public class ExportService
{
    private const string SolutionFileName = "PixelForge.sln";
    private readonly string _projectRoot;
    private readonly string _runtimeProjectPath;
    private readonly string _outputBasePath;

    public event EventHandler<string>? ProgressUpdate;

    public ExportService(string projectPath)
    {
        _projectRoot = ResolveProjectRoot(projectPath);
        _runtimeProjectPath = ResolveRuntimeProjectPath(projectPath);
        _outputBasePath = Path.Combine(_projectRoot, "Exports");
    }

    /// <summary>
    /// Export game for specified platform.
    /// </summary>
    public async Task<bool> ExportAsync(ExportOptions options)
    {
        try
        {
            ReportProgress("Starting export process...");

            // Create output directory
            var outputPath = Path.Combine(_outputBasePath, options.OutputName);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
            Directory.CreateDirectory(outputPath);

            // Build for target platform
            ReportProgress($"Building for {options.Platform}...");
            var buildSuccess = await PublishGameAsync(options, outputPath);

            if (!buildSuccess)
            {
                ReportProgress("Build failed!");
                return false;
            }

            // Copy project content/configuration
            ReportProgress("Copying project files...");
            if (!CopyProjectFiles(outputPath))
            {
                ReportProgress("Export failed: required project content missing.");
                return false;
            }

            // Create archive if requested
            if (options.CreateArchive)
            {
                ReportProgress("Creating archive...");
                CreateArchive(options, outputPath);
            }

            ReportProgress($"Export completed successfully! Output: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            ReportProgress($"Export error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Publish game using dotnet publish.
    /// </summary>
    private async Task<bool> PublishGameAsync(ExportOptions options, string outputPath)
    {
        try
        {
            var runtimeId = GetRuntimeIdentifier(options.Platform);
            var configuration = options.Configuration;

            var arguments = $"publish \"{_runtimeProjectPath}\" " +
                          $"--configuration {configuration} " +
                          $"--runtime {runtimeId} " +
                          $"--self-contained {options.SelfContained.ToString().ToLower()} " +
                          $"--output \"{outputPath}\"";

            if (options.SingleFile)
            {
                arguments += " -p:PublishSingleFile=true";
            }

            if (options.ReadyToRun)
            {
                arguments += " -p:PublishReadyToRun=true";
            }

            if (options.TrimUnusedCode)
            {
                arguments += " -p:PublishTrimmed=true";
            }

            var publishProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                WorkingDirectory = _projectRoot,
                UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            publishProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ReportProgress(e.Data);
                }
            };

            publishProcess.Start();
            publishProcess.BeginOutputReadLine();
            publishProcess.BeginErrorReadLine();

            await publishProcess.WaitForExitAsync();

            return publishProcess.ExitCode == 0;
        }
        catch (Exception ex)
        {
            ReportProgress($"Publish error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Copy content files to output directory.
    /// </summary>
    private bool CopyProjectFiles(string outputPath)
    {
        var projectRoot = ResolveProjectRoot(_projectRoot);
        if (!Directory.Exists(projectRoot))
        {
            ReportProgress($"Project root not found: {projectRoot}");
            return false;
        }

        var projectFilePath = ProjectManager.ProjectFilePath
            ?? Path.Combine(projectRoot, ProjectManager.ProjectFileName);

        if (!File.Exists(projectFilePath))
        {
            ReportProgress($"Project file not found: {projectFilePath}");
            return false;
        }

        var outputProjectFilePath = Path.Combine(outputPath, ProjectManager.ProjectFileName);
        File.Copy(projectFilePath, outputProjectFilePath, true);

        var project = ProjectManager.CurrentProject;
        var contentDirectories = project != null
            ? new[]
            {
                (Path.Combine(projectRoot, project.MapsPath), project.MapsPath, "Maps"),
                (Path.Combine(projectRoot, project.DatabasePath), project.DatabasePath, "Database"),
                (Path.Combine(projectRoot, project.AssetsPath), project.AssetsPath, "Assets")
            }
            : new[]
            {
                (Path.Combine(projectRoot, "Maps"), "Maps", "Maps"),
                (Path.Combine(projectRoot, "Database"), "Database", "Database"),
                (Path.Combine(projectRoot, "Assets"), "Assets", "Assets")
            };

        foreach (var (sourcePath, relativePath, label) in contentDirectories)
        {
            var destinationPath = Path.Combine(outputPath, relativePath);

            if (!Directory.Exists(sourcePath))
            {
                ReportProgress($"Source {label} folder missing. Creating empty folder: {destinationPath}");
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            CopyDirectory(sourcePath, destinationPath);
        }

        return ValidateExportOutput(outputPath, outputProjectFilePath, contentDirectories);
    }

    /// <summary>
    /// Create archive (zip) of the exported game.
    /// </summary>
    private void CreateArchive(ExportOptions options, string outputPath)
    {
        var archiveName = $"{options.OutputName}_{options.Platform}.zip";
        var archivePath = Path.Combine(_outputBasePath, archiveName);

        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        ZipFile.CreateFromDirectory(outputPath, archivePath, CompressionLevel.Optimal, false);
        ReportProgress($"Archive created: {archivePath}");
    }

    /// <summary>
    /// Get .NET runtime identifier for platform.
    /// </summary>
    private string GetRuntimeIdentifier(ExportPlatform platform)
    {
        return platform switch
        {
            ExportPlatform.Windows => "win-x64",
            ExportPlatform.Linux => "linux-x64",
            ExportPlatform.macOS => "osx-x64",
            ExportPlatform.WindowsArm => "win-arm64",
            ExportPlatform.LinuxArm => "linux-arm64",
            ExportPlatform.macOSArm => "osx-arm64",
            _ => throw new ArgumentException($"Unknown platform: {platform}")
        };
    }

    /// <summary>
    /// Copy directory recursively.
    /// </summary>
    private void CopyDirectory(string sourceDir, string destDir)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            return;

        Directory.CreateDirectory(destDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
            string newDestDir = Path.Combine(destDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestDir);
        }
    }

    private void ReportProgress(string message)
    {
        Console.WriteLine(message);
        ProgressUpdate?.Invoke(this, message);
    }

    private static string ResolveProjectRoot(string projectPath)
    {
        if (ProjectManager.ProjectRoot != null)
            return ProjectManager.ProjectRoot;

        return projectPath;
    }

    private static string ResolveRuntimeProjectPath(string projectPath)
    {
        var rootsToCheck = new[] { AppContext.BaseDirectory, projectPath };

        foreach (var root in rootsToCheck)
        {
            var solutionRoot = FindSolutionRoot(root);
            var gameProject = Path.Combine(solutionRoot, "src", "PixelForge.Game", "PixelForge.Game.csproj");
            if (File.Exists(gameProject))
                return gameProject;

            var editorProject = Path.Combine(solutionRoot, "src", "PixelForge.Editor", "PixelForge.Editor.csproj");
            if (File.Exists(editorProject))
                return editorProject;
        }

        throw new FileNotFoundException("Runtime game project not found. Expected PixelForge.Game or PixelForge.Editor project.");
    }

    private static string FindSolutionRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        if (File.Exists(startPath))
        {
            directory = new DirectoryInfo(Path.GetDirectoryName(startPath)!);
        }

        while (directory != null)
        {
            var solutionPath = Path.Combine(directory.FullName, SolutionFileName);
            if (File.Exists(solutionPath))
                return directory.FullName;

            directory = directory.Parent;
        }

        return startPath;
    }

    private bool ValidateExportOutput(
        string outputPath,
        string outputProjectFilePath,
        (string SourcePath, string RelativePath, string Label)[] contentDirectories)
    {
        var missingItems = contentDirectories
            .Select(item => (item.RelativePath, item.Label))
            .Where(item => !Directory.Exists(Path.Combine(outputPath, item.RelativePath)))
            .Select(item => item.Label)
            .ToList();

        if (!File.Exists(outputProjectFilePath))
        {
            missingItems.Add(ProjectManager.ProjectFileName);
        }

        if (missingItems.Count == 0)
            return true;

        ReportProgress($"Export validation failed. Missing: {string.Join(", ", missingItems)}");
        return false;
    }
}

/// <summary>
/// Export platform options.
/// </summary>
public enum ExportPlatform
{
    Windows,
    Linux,
    macOS,
    WindowsArm,
    LinuxArm,
    macOSArm
}

/// <summary>
/// Export configuration options.
/// </summary>
public class ExportOptions
{
    public string OutputName { get; set; } = "MyGame";
    public ExportPlatform Platform { get; set; } = ExportPlatform.Windows;
    public string Configuration { get; set; } = "Release";
    public bool SelfContained { get; set; } = true;
    public bool SingleFile { get; set; } = true;
    public bool ReadyToRun { get; set; } = true;
    public bool TrimUnusedCode { get; set; } = false;
    public bool CreateArchive { get; set; } = true;
}
