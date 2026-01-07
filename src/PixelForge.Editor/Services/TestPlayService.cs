using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PixelForge.Editor.Services;

/// <summary>
/// Service for launching and managing test play sessions.
/// </summary>
public class TestPlayService
{
    private Process? _gameProcess;
    private readonly string _gamePath;
    private readonly string _projectPath;

    public bool IsRunning => _gameProcess != null && !_gameProcess.HasExited;

    public event EventHandler? GameStarted;
    public event EventHandler? GameStopped;
    public event EventHandler<string>? OutputReceived;

    public TestPlayService(string projectPath)
    {
        _projectPath = projectPath;
        _gamePath = Path.Combine(projectPath, "src", "PixelForge.Game");
    }

    /// <summary>
    /// Start test play session.
    /// </summary>
    public async Task<bool> StartTestPlayAsync(TestPlayOptions? options = null)
    {
        if (IsRunning)
        {
            Console.WriteLine("Game is already running");
            return false;
        }

        options ??= new TestPlayOptions();

        try
        {
            // Build the game first if requested
            if (options.BuildBeforeRun)
            {
                var buildSuccess = await BuildGameAsync();
                if (!buildSuccess)
                {
                    Console.WriteLine("Build failed, cannot start test play");
                    return false;
                }
            }

            // Start the game process
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{_gamePath}\"",
                WorkingDirectory = _projectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Add custom arguments
            if (!string.IsNullOrEmpty(options.StartMap))
            {
                startInfo.Arguments += $" --start-map {options.StartMap}";
            }

            if (options.StartPosition != null)
            {
                startInfo.Arguments += $" --start-pos {options.StartPosition.X},{options.StartPosition.Y}";
            }

            if (options.DisableSaveLoad)
            {
                startInfo.Arguments += " --no-save";
            }

            _gameProcess = new Process { StartInfo = startInfo };

            // Hook up output events
            _gameProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputReceived?.Invoke(this, $"[OUT] {e.Data}");
                }
            };

            _gameProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputReceived?.Invoke(this, $"[ERR] {e.Data}");
                }
            };

            _gameProcess.Exited += (sender, e) =>
            {
                GameStopped?.Invoke(this, EventArgs.Empty);
            };

            _gameProcess.EnableRaisingEvents = true;

            if (_gameProcess.Start())
            {
                _gameProcess.BeginOutputReadLine();
                _gameProcess.BeginErrorReadLine();

                GameStarted?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("Test play started successfully");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting test play: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stop test play session.
    /// </summary>
    public void StopTestPlay()
    {
        if (_gameProcess == null || _gameProcess.HasExited)
            return;

        try
        {
            _gameProcess.Kill();
            _gameProcess.WaitForExit(5000);
            _gameProcess.Dispose();
            _gameProcess = null;

            Console.WriteLine("Test play stopped");
            GameStopped?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping test play: {ex.Message}");
        }
    }

    /// <summary>
    /// Build the game project.
    /// </summary>
    private async Task<bool> BuildGameAsync()
    {
        try
        {
            var buildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{_gamePath}\" --configuration Debug",
                    WorkingDirectory = _projectPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            buildProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputReceived?.Invoke(this, $"[BUILD] {e.Data}");
                }
            };

            buildProcess.Start();
            buildProcess.BeginOutputReadLine();
            buildProcess.BeginErrorReadLine();

            await buildProcess.WaitForExitAsync();

            return buildProcess.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Build error: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Options for test play session.
/// </summary>
public class TestPlayOptions
{
    public bool BuildBeforeRun { get; set; } = true;
    public string? StartMap { get; set; }
    public (int X, int Y)? StartPosition { get; set; }
    public bool DisableSaveLoad { get; set; }
    public bool EnableDebugMode { get; set; }
}
