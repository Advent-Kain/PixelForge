using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Editor.Services;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for asset browser (import and preview sprites, audio, etc.).
/// </summary>
public partial class AssetBrowserViewModel : ObservableObject
{
    private readonly IStorageProvider _storageProvider;
    private ObservableCollection<AssetItemViewModel> _allAssets = new();

    [ObservableProperty]
    private ObservableCollection<AssetCategory> _categories = new();

    [ObservableProperty]
    private AssetCategory? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<AssetItemViewModel> _assets = new();

    [ObservableProperty]
    private AssetItemViewModel? _selectedAsset;

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    public IRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ImportAssetCommand { get; }
    public IRelayCommand DeleteAssetCommand { get; }
    public IRelayCommand SearchCommand { get; }
    public IRelayCommand OpenInExplorerCommand { get; }

    public AssetBrowserViewModel(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
        RefreshCommand = new RelayCommand(Refresh);
        ImportAssetCommand = new AsyncRelayCommand(ImportAssetAsync, CanImportAsset);
        DeleteAssetCommand = new RelayCommand(DeleteAsset, CanDeleteAsset);
        SearchCommand = new RelayCommand(ApplySearch);
        OpenInExplorerCommand = new RelayCommand(OpenInExplorer, CanOpenInExplorer);

        InitializeCategories();
        Refresh();
    }

    private void InitializeCategories()
    {
        Categories.Add(new AssetCategory
        {
            Name = "Characters",
            Path = "Graphics/Characters",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Animations",
            Path = "Graphics/Animations",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Tilesets",
            Path = "Graphics/Tilesets",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Faces",
            Path = "Graphics/Faces",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Enemies",
            Path = "Graphics/Enemies",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Battlebacks1",
            Path = "Graphics/Battlebacks1",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Battlebacks2",
            Path = "Graphics/Battlebacks2",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Parallaxes",
            Path = "Graphics/Parallaxes",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Pictures",
            Path = "Graphics/Pictures",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "SV Actors",
            Path = "Graphics/SV_Actors",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "SV Enemies",
            Path = "Graphics/SV_Enemies",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Titles1",
            Path = "Graphics/Titles1",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "Titles2",
            Path = "Graphics/Titles2",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "UI",
            Path = "Graphics/UI",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "System",
            Path = "Graphics/System",
            Extensions = new[] { ".png", ".jpg", ".jpeg" },
            Type = AssetType.Image
        });

        Categories.Add(new AssetCategory
        {
            Name = "BGM",
            Path = "Audio/BGM",
            Extensions = new[] { ".mp3", ".ogg", ".wav" },
            Type = AssetType.Audio
        });

        Categories.Add(new AssetCategory
        {
            Name = "BGS",
            Path = "Audio/BGS",
            Extensions = new[] { ".mp3", ".ogg", ".wav" },
            Type = AssetType.Audio
        });

        Categories.Add(new AssetCategory
        {
            Name = "ME",
            Path = "Audio/ME",
            Extensions = new[] { ".mp3", ".ogg", ".wav" },
            Type = AssetType.Audio
        });

        Categories.Add(new AssetCategory
        {
            Name = "SE",
            Path = "Audio/SE",
            Extensions = new[] { ".mp3", ".ogg", ".wav" },
            Type = AssetType.Audio
        });

        SelectedCategory = Categories.FirstOrDefault();
    }

    partial void OnSelectedCategoryChanged(AssetCategory? value)
    {
        if (value != null)
        {
            LoadAssetsForCategory(value);
        }

        ImportAssetCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedAssetChanged(AssetItemViewModel? value)
    {
        if (value != null && value.Type == AssetType.Image)
        {
            LoadPreviewImage(value);
        }
        else
        {
            PreviewImage = null;
        }

        DeleteAssetCommand.NotifyCanExecuteChanged();
        OpenInExplorerCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchFilterChanged(string value)
    {
        ApplySearch();
    }

    private void Refresh()
    {
        if (SelectedCategory != null)
        {
            LoadAssetsForCategory(SelectedCategory);
        }
    }

    private void LoadAssetsForCategory(AssetCategory category)
    {
        Assets.Clear();

        var fullPath = Path.Combine(GetAssetsRoot(), category.Path);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories)
            .Where(f => category.Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => Path.GetFileName(f));

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            var relativePath = Path.GetRelativePath(GetAssetsRoot(), file);

            Assets.Add(new AssetItemViewModel
            {
                Name = Path.GetFileNameWithoutExtension(file),
                FileName = Path.GetFileName(file),
                FilePath = file,
                RelativePath = relativePath,
                Type = category.Type,
                Size = FormatFileSize(fileInfo.Length),
                DateModified = fileInfo.LastWriteTime
            });
        }

        _allAssets = new ObservableCollection<AssetItemViewModel>(Assets);
        ApplySearch();
    }

    private void LoadPreviewImage(AssetItemViewModel asset)
    {
        try
        {
            if (File.Exists(asset.FilePath))
            {
                PreviewImage = new Bitmap(asset.FilePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading preview: {ex.Message}");
            PreviewImage = null;
        }
    }

    private bool CanImportAsset() => SelectedCategory != null;

    private async System.Threading.Tasks.Task ImportAssetAsync()
    {
        if (SelectedCategory == null)
            return;

        try
        {
            var options = new FilePickerOpenOptions
            {
                Title = $"Import {SelectedCategory.Name}",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType($"{SelectedCategory.Name} files")
                    {
                        Patterns = SelectedCategory.Extensions.Select(ext => $"*{ext}").ToArray()
                    }
                }
            };

            var results = await _storageProvider.OpenFilePickerAsync(options);
            if (results.Count == 0)
            {
                return;
            }

            var destinationRoot = Path.Combine(GetAssetsRoot(), SelectedCategory.Path);
            Directory.CreateDirectory(destinationRoot);

            foreach (var file in results)
            {
                var fileName = file.Name;
                var destinationPath = Path.Combine(destinationRoot, fileName);

                var localPath = file.TryGetLocalPath();
                if (!string.IsNullOrWhiteSpace(localPath) && File.Exists(localPath))
                {
                    File.Copy(localPath, destinationPath, true);
                    continue;
                }

                await using var sourceStream = await file.OpenReadAsync();
                await using var destinationStream = File.Create(destinationPath);
                await sourceStream.CopyToAsync(destinationStream);
            }

            Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing asset: {ex.Message}");
        }
    }

    private bool CanDeleteAsset() => SelectedAsset != null;

    private void DeleteAsset()
    {
        if (SelectedAsset == null)
            return;

        try
        {
            if (File.Exists(SelectedAsset.FilePath))
            {
                File.Delete(SelectedAsset.FilePath);
                Assets.Remove(SelectedAsset);
                SelectedAsset = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting asset: {ex.Message}");
        }
    }

    private void ApplySearch()
    {
        if (string.IsNullOrWhiteSpace(SearchFilter))
        {
            Assets.Clear();
            foreach (var asset in _allAssets)
            {
                Assets.Add(asset);
            }
            return;
        }

        // Filter assets based on search
        var filtered = _allAssets.Where(a =>
            a.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
            a.FileName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        // Update collection
        Assets.Clear();
        foreach (var asset in filtered)
        {
            Assets.Add(asset);
        }
    }

    private bool CanOpenInExplorer() => SelectedAsset != null;

    private void OpenInExplorer()
    {
        if (SelectedAsset == null)
            return;

        try
        {
            var folder = Path.GetDirectoryName(SelectedAsset.FilePath);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening in explorer: {ex.Message}");
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private string GetAssetsRoot()
    {
        return ProjectManager.GetAssetsDirectory()
            ?? Path.Combine(Environment.CurrentDirectory, "Assets");
    }
}

/// <summary>
/// Asset category.
/// </summary>
public class AssetCategory
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string[] Extensions { get; set; } = Array.Empty<string>();
    public AssetType Type { get; set; }
}

/// <summary>
/// Asset item view model.
/// </summary>
public partial class AssetItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _relativePath = string.Empty;

    [ObservableProperty]
    private AssetType _type;

    [ObservableProperty]
    private string _size = string.Empty;

    [ObservableProperty]
    private DateTime _dateModified;
}

/// <summary>
/// Asset type.
/// </summary>
public enum AssetType
{
    Image,
    Audio,
    Data
}
