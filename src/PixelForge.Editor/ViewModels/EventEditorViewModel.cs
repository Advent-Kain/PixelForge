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

        var command = new EventCommand();
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

public partial class EventCommandViewModel : ObservableObject
{
    private readonly EventCommand _model;

    [ObservableProperty]
    private int _code;

    [ObservableProperty]
    private int _indent;

    [ObservableProperty]
    private string _parametersText = "[]";

    public EventCommandViewModel(EventCommand model)
    {
        _model = model;
        _code = model.Code;
        _indent = model.Indent;
        _parametersText = SerializeParameters(model.Parameters);
    }

    public EventCommand Model => _model;

    partial void OnCodeChanged(int value)
    {
        _model.Code = value;
    }

    partial void OnIndentChanged(int value)
    {
        _model.Indent = value;
    }

    partial void OnParametersTextChanged(string value)
    {
        var parsed = ParseParameters(value);
        if (parsed != null)
            _model.Parameters = parsed;
    }

    private static string SerializeParameters(List<object> parameters)
    {
        if (parameters.Count == 0)
            return "[]";

        return JsonSerializer.Serialize(parameters);
    }

    private static List<object>? ParseParameters(string value)
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
}
