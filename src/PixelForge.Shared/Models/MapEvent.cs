using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PixelForge.Shared.Models;

/// <summary>
/// Represents an event on a map.
/// </summary>
public class MapEvent
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Event name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Event";

    /// <summary>
    /// Position on the map (in tiles).
    /// </summary>
    [JsonPropertyName("position")]
    public Vector2Int Position { get; set; }

    /// <summary>
    /// Event pages (conditions and commands).
    /// </summary>
    [JsonPropertyName("pages")]
    public List<EventPage> Pages { get; set; } = new();

    /// <summary>
    /// Notes for the editor.
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Represents a page of an event with conditions and commands.
/// </summary>
public class EventPage
{
    /// <summary>
    /// Conditions that must be met for this page to be active.
    /// </summary>
    [JsonPropertyName("conditions")]
    public EventConditions Conditions { get; set; } = new();

    /// <summary>
    /// Sprite graphic for this event page.
    /// </summary>
    [JsonPropertyName("graphic")]
    public EventGraphic Graphic { get; set; } = new();

    /// <summary>
    /// Movement settings.
    /// </summary>
    [JsonPropertyName("movement")]
    public EventMovement Movement { get; set; } = new();

    /// <summary>
    /// Trigger type for this event.
    /// </summary>
    [JsonPropertyName("trigger")]
    public EventTrigger Trigger { get; set; } = EventTrigger.ActionButton;

    /// <summary>
    /// Priority level.
    /// </summary>
    [JsonPropertyName("priority")]
    public EventPriority Priority { get; set; } = EventPriority.Normal;

    /// <summary>
    /// List of commands to execute.
    /// </summary>
    [JsonPropertyName("commands")]
    public List<EventCommand> Commands { get; set; } = new();
}

/// <summary>
/// Conditions for an event page.
/// </summary>
public class EventConditions
{
    [JsonPropertyName("switch1")]
    public int? Switch1 { get; set; }

    [JsonPropertyName("switch2")]
    public int? Switch2 { get; set; }

    [JsonPropertyName("variable")]
    public int? Variable { get; set; }

    [JsonPropertyName("variableValue")]
    public int? VariableValue { get; set; }

    [JsonPropertyName("selfSwitch")]
    public string? SelfSwitch { get; set; }

    [JsonPropertyName("item")]
    public string? Item { get; set; }

    [JsonPropertyName("actor")]
    public string? Actor { get; set; }
}

/// <summary>
/// Graphic settings for an event.
/// </summary>
public class EventGraphic
{
    [JsonPropertyName("characterName")]
    public string? CharacterName { get; set; }

    [JsonPropertyName("characterIndex")]
    public int CharacterIndex { get; set; }

    [JsonPropertyName("direction")]
    public int Direction { get; set; } = 2; // Down

    [JsonPropertyName("pattern")]
    public int Pattern { get; set; }
}

/// <summary>
/// Movement settings for an event.
/// </summary>
public class EventMovement
{
    [JsonPropertyName("type")]
    public MovementType Type { get; set; } = MovementType.Fixed;

    [JsonPropertyName("speed")]
    public int Speed { get; set; } = 3;

    [JsonPropertyName("frequency")]
    public int Frequency { get; set; } = 3;

    [JsonPropertyName("route")]
    public List<MoveCommand>? Route { get; set; }
}

/// <summary>
/// A single event command.
/// </summary>
public class EventCommand
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("indent")]
    public int Indent { get; set; }

    [JsonPropertyName("parameters")]
    public List<object> Parameters { get; set; } = new();
}

/// <summary>
/// A movement command.
/// </summary>
public class MoveCommand
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("parameters")]
    public List<object> Parameters { get; set; } = new();
}

/// <summary>
/// A movement route definition.
/// </summary>
public class MoveRoute
{
    [JsonPropertyName("repeat")]
    public bool Repeat { get; set; }

    [JsonPropertyName("skippable")]
    public bool Skippable { get; set; }

    [JsonPropertyName("wait")]
    public bool Wait { get; set; }

    [JsonPropertyName("list")]
    public List<MoveCommand> Commands { get; set; } = new();
}

/// <summary>
/// Event trigger types.
/// </summary>
public enum EventTrigger
{
    ActionButton = 0,
    PlayerTouch = 1,
    EventTouch = 2,
    Autorun = 3,
    Parallel = 4
}

/// <summary>
/// Event priority levels.
/// </summary>
public enum EventPriority
{
    Below = 0,
    Normal = 1,
    Above = 2
}

/// <summary>
/// Movement types.
/// </summary>
public enum MovementType
{
    Fixed = 0,
    Random = 1,
    Approach = 2,
    Custom = 3
}
