using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Editor.Services;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for export/packaging window.
/// </summary>
public partial class ExportViewModel : ObservableObject
{
    private readonly ExportService _exportService;

    [ObservableProperty]
    private string _outputName = "MyGame";

    [ObservableProperty]
    private ExportPlatform _selectedPlatform = ExportPlatform.Windows;

    [ObservableProperty]
    private string _configuration = "Release";

    [ObservableProperty]
    private bool _selfContained = true;

    [ObservableProperty]
    private bool _singleFile = true;

    [ObservableProperty]
    private bool _readyToRun = true;

    [ObservableProperty]
    private bool _trimUnusedCode = false;

    [ObservableProperty]
    private bool _createArchive = true;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private ObservableCollection<string> _exportLog = new();

    public ICommand ExportCommand { get; }
    public ICommand ClearLogCommand { get; }

    public ExportViewModel()
    {
        var projectRoot = ProjectManager.ProjectRoot ?? Environment.CurrentDirectory;
        _exportService = new ExportService(projectRoot);
        _exportService.ProgressUpdate += OnProgressUpdate;

        ExportCommand = new RelayCommand(async () => await ExportAsync(), () => !IsExporting);
        ClearLogCommand = new RelayCommand(ClearLog);
    }

    private async System.Threading.Tasks.Task ExportAsync()
    {
        IsExporting = true;
        ClearLog();

        var options = new ExportOptions
        {
            OutputName = OutputName,
            Platform = SelectedPlatform,
            Configuration = Configuration,
            SelfContained = SelfContained,
            SingleFile = SingleFile,
            ReadyToRun = ReadyToRun,
            TrimUnusedCode = TrimUnusedCode,
            CreateArchive = CreateArchive
        };

        await _exportService.ExportAsync(options);

        IsExporting = false;
    }

    private void OnProgressUpdate(object? sender, string message)
    {
        ExportLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    private void ClearLog()
    {
        ExportLog.Clear();
    }
}
