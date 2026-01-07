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
    private readonly string _projectPath;
    private readonly string _gamePath;
    private readonly string _outputBasePath;

    public event EventHandler<string>? ProgressUpdate;

    public ExportService(string projectPath)
    {
        _projectPath = projectPath;
        _gamePath = Path.Combine(projectPath, "src", "PixelForge.Game");
        _outputBasePath = Path.Combine(projectPath, "Exports");
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

            // Copy content files
            ReportProgress("Copying content files...");
            CopyContentFiles(outputPath);

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

            var arguments = $"publish \"{_gamePath}\" " +
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
                    WorkingDirectory = _projectPath,
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
    private void CopyContentFiles(string outputPath)
    {
        var contentPath = Path.Combine(_projectPath, "Content");
        if (!Directory.Exists(contentPath))
            return;

        var destContentPath = Path.Combine(outputPath, "Content");
        Directory.CreateDirectory(destContentPath);

        CopyDirectory(contentPath, destContentPath);
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
