using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Shared.Models;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for editing map events and commands.
/// </summary>
public partial class EventEditorViewModel : ViewModelBase
{
    private readonly MapData? _map;

    [ObservableProperty]
    private MapEvent? _selectedEvent;

    [ObservableProperty]
    private EventPage? _selectedPage;

    [ObservableProperty]
    private EventCommandViewModel? _selectedCommand;

    [ObservableProperty]
    private string _eventName = string.Empty;

    [ObservableProperty]
    private string _eventNote = string.Empty;

    [ObservableProperty]
    private int _positionX;

    [ObservableProperty]
    private int _positionY;

    public ObservableCollection<MapEvent> Events { get; } = new();

    public ObservableCollection<EventPage> Pages { get; } = new();

    public ObservableCollection<EventCommandViewModel> Commands { get; } = new();

    public IReadOnlyList<EventTrigger> TriggerOptions { get; } = Enum.GetValues<EventTrigger>();

    public IReadOnlyList<EventPriority> PriorityOptions { get; } = Enum.GetValues<EventPriority>();

    public EventEditorViewModel()
        : this(MapData.CreateDefault())
    {
    }

    public EventEditorViewModel(MapData? map)
    {
        _map = map;

        if (_map == null)
            return;

        foreach (var mapEvent in _map.Events)
        {
            Events.Add(mapEvent);
        }

        SelectedEvent = Events.FirstOrDefault();
    }

    [RelayCommand]
    private void AddEvent()
    {
        if (_map == null)
            return;

        var newEvent = new MapEvent
        {
            Name = $"Event {Events.Count + 1}",
            Position = Vector2Int.Zero
        };
        newEvent.Pages.Add(new EventPage());

        _map.Events.Add(newEvent);
        Events.Add(newEvent);
        SelectedEvent = newEvent;
    }

    [RelayCommand]
    private void RemoveEvent()
    {
        if (_map == null || SelectedEvent == null)
            return;

        _map.Events.Remove(SelectedEvent);
        Events.Remove(SelectedEvent);
        SelectedEvent = Events.FirstOrDefault();
    }

    [RelayCommand]
    private void AddPage()
    {
        if (SelectedEvent == null)
            return;

        var newPage = new EventPage();
        SelectedEvent.Pages.Add(newPage);
        Pages.Add(newPage);
        SelectedPage = newPage;
    }

    [RelayCommand]
    private void RemovePage()
    {
        if (SelectedEvent == null || SelectedPage == null)
            return;

        SelectedEvent.Pages.Remove(SelectedPage);
        Pages.Remove(SelectedPage);
        SelectedPage = Pages.FirstOrDefault();
    }

    [RelayCommand]
    private void AddCommand()
    {
        if (SelectedPage == null)
            return;

        var command = new EventCommand
        {
            Code = EventCommandTypeMapping.GetCode(EventCommandType.ShowMessage),
            Parameters = new List<object> { string.Empty }
        };
        SelectedPage.Commands.Add(command);
        var viewModel = new EventCommandViewModel(command);
        Commands.Add(viewModel);
        SelectedCommand = viewModel;
    }

    [RelayCommand]
    private void RemoveCommand()
    {
        if (SelectedPage == null || SelectedCommand == null)
            return;

        SelectedPage.Commands.Remove(SelectedCommand.Model);
        Commands.Remove(SelectedCommand);
        SelectedCommand = Commands.FirstOrDefault();
    }

    partial void OnSelectedEventChanged(MapEvent? value)
    {
        Pages.Clear();
        Commands.Clear();
        SelectedPage = null;

        if (value == null)
        {
            EventName = string.Empty;
            EventNote = string.Empty;
            PositionX = 0;
            PositionY = 0;
            return;
        }

        EventName = value.Name;
        EventNote = value.Note ?? string.Empty;
        PositionX = value.Position.X;
        PositionY = value.Position.Y;

        foreach (var page in value.Pages)
        {
            Pages.Add(page);
        }

        SelectedPage = Pages.FirstOrDefault();
    }

    partial void OnSelectedPageChanged(EventPage? value)
    {
        Commands.Clear();
        SelectedCommand = null;

        if (value == null)
            return;

        foreach (var command in value.Commands)
        {
            Commands.Add(new EventCommandViewModel(command));
        }

        SelectedCommand = Commands.FirstOrDefault();
    }

    partial void OnEventNameChanged(string value)
    {
        if (SelectedEvent != null)
            SelectedEvent.Name = value;
    }

    partial void OnEventNoteChanged(string value)
    {
        if (SelectedEvent != null)
            SelectedEvent.Note = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    partial void OnPositionXChanged(int value)
    {
        if (SelectedEvent != null)
            SelectedEvent.Position = new Vector2Int(value, PositionY);
    }

    partial void OnPositionYChanged(int value)
    {
        if (SelectedEvent != null)
            SelectedEvent.Position = new Vector2Int(PositionX, value);
    }
}

public class EventCommandViewModel : ObservableObject
{
    private readonly EventCommand _model;
    private int _code;
    private int _indent;
    private EventCommandType _commandType;
    private EventCommandParametersViewModel _parametersViewModel;

    public EventCommandViewModel(EventCommand model)
    {
        _model = model;
        _code = model.Code;
        _indent = model.Indent;
        _commandType = EventCommandTypeMapping.GetTypeForCode(model.Code);
        _parametersViewModel = CreateParametersViewModel(_commandType);
        _parametersViewModel.LoadFromParameters(model.Parameters);
        _parametersViewModel.PropertyChanged += ParametersViewModelOnPropertyChanged;
    }

    public EventCommand Model => _model;

    public IReadOnlyList<EventCommandType> CommandTypeOptions { get; } = Enum.GetValues<EventCommandType>();

    public int Code
    {
        get => _code;
        set
        {
            if (!SetProperty(ref _code, value))
                return;

            _model.Code = value;
        }
    }

    public int Indent
    {
        get => _indent;
        set
        {
            if (!SetProperty(ref _indent, value))
                return;

            _model.Indent = value;
        }
    }

    public EventCommandType CommandType
    {
        get => _commandType;
        set
        {
            if (!SetProperty(ref _commandType, value))
                return;

            if (value != EventCommandType.Custom)
                Code = EventCommandTypeMapping.GetCode(value);

            ParametersViewModel = CreateParametersViewModel(value);
            UpdateModelParameters();
            OnPropertyChanged(nameof(IsCustom));
            OnPropertyChanged(nameof(Summary));
        }
    }

    public bool IsCustom => CommandType == EventCommandType.Custom;

    public EventCommandParametersViewModel ParametersViewModel
    {
        get => _parametersViewModel;
        private set
        {
            if (_parametersViewModel == value)
                return;

            if (_parametersViewModel != null)
                _parametersViewModel.PropertyChanged -= ParametersViewModelOnPropertyChanged;

            _parametersViewModel = value;
            if (_parametersViewModel != null)
                _parametersViewModel.PropertyChanged += ParametersViewModelOnPropertyChanged;

            OnPropertyChanged();
        }
    }

    public string Summary => ParametersViewModel?.Summary ?? string.Empty;

    private void ParametersViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateModelParameters();
        OnPropertyChanged(nameof(Summary));
    }

    private void UpdateModelParameters()
    {
        _model.Parameters = ParametersViewModel.ToParameters();
    }

    private static EventCommandParametersViewModel CreateParametersViewModel(EventCommandType commandType)
    {
        return commandType switch
        {
            EventCommandType.ShowMessage => new ShowMessageParametersViewModel(),
            EventCommandType.ShowChoices => new ShowChoicesParametersViewModel(),
            EventCommandType.InputNumber => new InputNumberParametersViewModel(),
            EventCommandType.Wait => new WaitParametersViewModel(),
            EventCommandType.Script => new ScriptParametersViewModel(),
            _ => new CustomParametersViewModel()
        };
    }
}

public enum EventCommandType
{
    ShowMessage,
    ShowChoices,
    InputNumber,
    Wait,
    Script,
    Custom
}

public abstract class EventCommandParametersViewModel : ObservableObject
{
    public abstract EventCommandType CommandType { get; }

    public virtual string Summary => string.Empty;

    public abstract List<object> ToParameters();

    public abstract void LoadFromParameters(List<object> parameters);
}

public class ShowMessageParametersViewModel : EventCommandParametersViewModel
{
    private string _text = string.Empty;

    public override EventCommandType CommandType => EventCommandType.ShowMessage;

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public override string Summary => Text;

    public override List<object> ToParameters() => new() { Text };

    public override void LoadFromParameters(List<object> parameters)
    {
        Text = EventCommandParameterConverter.GetString(parameters.ElementAtOrDefault(0), string.Empty);
    }
}

public class ShowChoicesParametersViewModel : EventCommandParametersViewModel
{
    private string _choicesText = string.Empty;

    public override EventCommandType CommandType => EventCommandType.ShowChoices;

    public string ChoicesText
    {
        get => _choicesText;
        set => SetProperty(ref _choicesText, value);
    }

    public override string Summary
    {
        get
        {
            var choices = GetChoices();
            if (choices.Count == 0)
                return "No choices";

            return string.Join(", ", choices);
        }
    }

    public override List<object> ToParameters() => new()
    {
        GetChoices()
    };

    public override void LoadFromParameters(List<object> parameters)
    {
        var choices = EventCommandParameterConverter.GetStringList(parameters.ElementAtOrDefault(0));
        ChoicesText = string.Join(Environment.NewLine, choices);
    }

    private List<string> GetChoices()
    {
        if (string.IsNullOrWhiteSpace(ChoicesText))
            return new List<string>();

        return ChoicesText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}

public class InputNumberParametersViewModel : EventCommandParametersViewModel
{
    private int _variableId;
    private int _digits = 4;

    public override EventCommandType CommandType => EventCommandType.InputNumber;

    public int VariableId
    {
        get => _variableId;
        set => SetProperty(ref _variableId, value);
    }

    public int Digits
    {
        get => _digits;
        set => SetProperty(ref _digits, value);
    }

    public override string Summary => $"Variable {VariableId}, {Digits} digits";

    public override List<object> ToParameters() => new()
    {
        VariableId,
        Digits
    };

    public override void LoadFromParameters(List<object> parameters)
    {
        VariableId = EventCommandParameterConverter.GetInt(parameters.ElementAtOrDefault(0), 0);
        Digits = EventCommandParameterConverter.GetInt(parameters.ElementAtOrDefault(1), 4);
    }
}

public class WaitParametersViewModel : EventCommandParametersViewModel
{
    private int _durationFrames;

    public override EventCommandType CommandType => EventCommandType.Wait;

    public int DurationFrames
    {
        get => _durationFrames;
        set => SetProperty(ref _durationFrames, value);
    }

    public override string Summary => $"{DurationFrames} frames";

    public override List<object> ToParameters() => new()
    {
        DurationFrames
    };

    public override void LoadFromParameters(List<object> parameters)
    {
        DurationFrames = EventCommandParameterConverter.GetInt(parameters.ElementAtOrDefault(0), 0);
    }
}

public class ScriptParametersViewModel : EventCommandParametersViewModel
{
    private string _script = string.Empty;

    public override EventCommandType CommandType => EventCommandType.Script;

    public string Script
    {
        get => _script;
        set => SetProperty(ref _script, value);
    }

    public override string Summary => string.IsNullOrWhiteSpace(Script) ? "Script" : "Script (custom)";

    public override List<object> ToParameters() => new() { Script };

    public override void LoadFromParameters(List<object> parameters)
    {
        Script = EventCommandParameterConverter.GetString(parameters.ElementAtOrDefault(0), string.Empty);
    }
}

public class CustomParametersViewModel : EventCommandParametersViewModel
{
    private string _parametersText = "[]";
    private List<object> _parsedParameters = new();

    public override EventCommandType CommandType => EventCommandType.Custom;

    public string ParametersText
    {
        get => _parametersText;
        set
        {
            if (!SetProperty(ref _parametersText, value))
                return;

            var parsed = EventCommandParameterConverter.ParseParameters(value);
            if (parsed != null)
                _parsedParameters = parsed;
        }
    }

    public override string Summary => ParametersText;

    public override List<object> ToParameters() => new(_parsedParameters);

    public override void LoadFromParameters(List<object> parameters)
    {
        _parsedParameters = new List<object>(parameters);
        _parametersText = EventCommandParameterConverter.SerializeParameters(parameters);
        OnPropertyChanged(nameof(ParametersText));
    }
}

internal static class EventCommandTypeMapping
{
    private static readonly Dictionary<int, EventCommandType> CodeToType = new()
    {
        [101] = EventCommandType.ShowMessage,
        [102] = EventCommandType.ShowChoices,
        [103] = EventCommandType.InputNumber,
        [230] = EventCommandType.Wait,
        [355] = EventCommandType.Script
    };

    public static EventCommandType GetTypeForCode(int code)
    {
        return CodeToType.TryGetValue(code, out var type) ? type : EventCommandType.Custom;
    }

    public static int GetCode(EventCommandType commandType)
    {
        return commandType switch
        {
            EventCommandType.ShowMessage => 101,
            EventCommandType.ShowChoices => 102,
            EventCommandType.InputNumber => 103,
            EventCommandType.Wait => 230,
            EventCommandType.Script => 355,
            _ => 0
        };
    }
}

internal static class EventCommandParameterConverter
{
    public static string SerializeParameters(List<object> parameters)
    {
        return parameters.Count == 0 ? "[]" : JsonSerializer.Serialize(parameters);
    }

    public static List<object>? ParseParameters(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<object>();

        var trimmed = value.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                return JsonSerializer.Deserialize<List<object>>(trimmed) ?? new List<object>();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        var entries = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return entries.Select(entry => (object)entry).ToList();
    }

    public static string GetString(object? value, string fallback)
    {
        if (value == null)
            return fallback;

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? fallback,
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => element.ToString()
            };
        }

        return value.ToString() ?? fallback;
    }

    public static int GetInt(object? value, int fallback)
    {
        if (value == null)
            return fallback;

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
                return number;

            if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out number))
                return number;
        }

        if (value is int intValue)
            return intValue;

        if (int.TryParse(value.ToString(), out var parsed))
            return parsed;

        return fallback;
    }

    public static List<string> GetStringList(object? value)
    {
        if (value == null)
            return new List<string>();

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .Select(entry => GetString(entry, string.Empty))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();
        }

        if (value is IEnumerable<string> stringList)
            return stringList.ToList();

        if (value is IEnumerable<object> objectList)
        {
            return objectList
                .Select(entry => GetString(entry, string.Empty))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();
        }

        var single = GetString(value, string.Empty);
        return string.IsNullOrWhiteSpace(single) ? new List<string>() : new List<string> { single };
    }
}
