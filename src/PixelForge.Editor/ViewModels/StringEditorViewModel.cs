using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for string file editor (localization).
/// </summary>
public partial class StringEditorViewModel : ObservableObject
{
    private readonly string _stringsPath = "Content/Strings";

    [ObservableProperty]
    private ObservableCollection<LanguageFileInfo> _languageFiles = new();

    [ObservableProperty]
    private LanguageFileInfo? _selectedLanguage;

    [ObservableProperty]
    private ObservableCollection<StringEntryViewModel> _strings = new();

    [ObservableProperty]
    private StringEntryViewModel? _selectedString;

    [ObservableProperty]
    private string _newLanguageCode = string.Empty;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    public ICommand NewLanguageCommand { get; }
    public ICommand LoadLanguageCommand { get; }
    public ICommand SaveLanguageCommand { get; }
    public ICommand DeleteLanguageCommand { get; }
    public ICommand AddStringCommand { get; }
    public ICommand DeleteStringCommand { get; }
    public ICommand SearchCommand { get; }

    public StringEditorViewModel()
    {
        NewLanguageCommand = new RelayCommand(CreateNewLanguage);
        LoadLanguageCommand = new RelayCommand(LoadLanguage, CanLoadLanguage);
        SaveLanguageCommand = new RelayCommand(SaveLanguage, CanSaveLanguage);
        DeleteLanguageCommand = new RelayCommand(DeleteLanguage, CanDeleteLanguage);
        AddStringCommand = new RelayCommand(AddString);
        DeleteStringCommand = new RelayCommand(DeleteString, CanDeleteString);
        SearchCommand = new RelayCommand(ApplySearch);

        LoadLanguageFiles();
    }

    private void LoadLanguageFiles()
    {
        LanguageFiles.Clear();

        if (!Directory.Exists(_stringsPath))
            Directory.CreateDirectory(_stringsPath);

        var files = Directory.GetFiles(_stringsPath, "*.json");
        foreach (var file in files)
        {
            var code = Path.GetFileNameWithoutExtension(file);
            LanguageFiles.Add(new LanguageFileInfo
            {
                LanguageCode = code,
                FilePath = file
            });
        }

        // Create default English if no files exist
        if (LanguageFiles.Count == 0)
        {
            CreateDefaultLanguage();
        }
    }

    private void CreateDefaultLanguage()
    {
        var defaultStrings = new Dictionary<string, string>
        {
            { "system.start", "Start Game" },
            { "system.continue", "Continue" },
            { "system.quit", "Quit" },
            { "dialogue.example.greeting", "Hello, adventurer!" }
        };

        var filePath = Path.Combine(_stringsPath, "en.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(defaultStrings, options);
        File.WriteAllText(filePath, json);

        LoadLanguageFiles();
    }

    private void CreateNewLanguage()
    {
        if (string.IsNullOrWhiteSpace(NewLanguageCode))
            return;

        var filePath = Path.Combine(_stringsPath, $"{NewLanguageCode}.json");

        if (File.Exists(filePath))
        {
            Console.WriteLine($"Language file already exists: {NewLanguageCode}");
            return;
        }

        var defaultStrings = new Dictionary<string, string>();
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(defaultStrings, options);
        File.WriteAllText(filePath, json);

        NewLanguageCode = string.Empty;
        LoadLanguageFiles();
    }

    private bool CanLoadLanguage() => SelectedLanguage != null;

    private void LoadLanguage()
    {
        if (SelectedLanguage == null)
            return;

        try
        {
            var json = File.ReadAllText(SelectedLanguage.FilePath);
            var stringDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            Strings.Clear();
            if (stringDict != null)
            {
                foreach (var kvp in stringDict.OrderBy(x => x.Key))
                {
                    Strings.Add(new StringEntryViewModel
                    {
                        Key = kvp.Key,
                        Value = kvp.Value
                    });
                }
            }

            ApplySearch();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading language file: {ex.Message}");
        }
    }

    private bool CanSaveLanguage() => SelectedLanguage != null;

    private void SaveLanguage()
    {
        if (SelectedLanguage == null)
            return;

        try
        {
            var stringDict = new Dictionary<string, string>();
            foreach (var entry in Strings)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key))
                {
                    stringDict[entry.Key] = entry.Value ?? string.Empty;
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(stringDict, options);
            File.WriteAllText(SelectedLanguage.FilePath, json);

            Console.WriteLine($"Saved language file: {SelectedLanguage.LanguageCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving language file: {ex.Message}");
        }
    }

    private bool CanDeleteLanguage() => SelectedLanguage != null;

    private void DeleteLanguage()
    {
        if (SelectedLanguage == null)
            return;

        try
        {
            File.Delete(SelectedLanguage.FilePath);
            LoadLanguageFiles();
            Strings.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting language file: {ex.Message}");
        }
    }

    private void AddString()
    {
        var newEntry = new StringEntryViewModel
        {
            Key = $"new.string.{Strings.Count + 1}",
            Value = "New string value..."
        };

        Strings.Add(newEntry);
        SelectedString = newEntry;
    }

    private bool CanDeleteString() => SelectedString != null;

    private void DeleteString()
    {
        if (SelectedString == null)
            return;

        Strings.Remove(SelectedString);
        SelectedString = null;
    }

    private void ApplySearch()
    {
        // Search filtering could be implemented here
        // For now, all strings are shown
    }
}

/// <summary>
/// Language file info.
/// </summary>
public class LanguageFileInfo
{
    public string LanguageCode { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// View model for a string entry.
/// </summary>
public partial class StringEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string? _value;

    [ObservableProperty]
    private string? _notes;
}
