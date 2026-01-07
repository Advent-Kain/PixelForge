using Microsoft.Xna.Framework;

namespace PixelForge.Engine.Core;

/// <summary>
/// Manages the global game state including switches, variables, and player data.
/// </summary>
public class GameState
{
    private readonly Dictionary<int, bool> _switches = new();
    private readonly Dictionary<int, int> _variables = new();
    private readonly Dictionary<string, bool> _selfSwitches = new();

    public string? CurrentMapId { get; set; }
    public TimeSpan PlayTime { get; private set; }
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public int PartyGold { get; set; }
    public List<string> PartyMembers { get; } = new();
    public Dictionary<string, int> Inventory { get; } = new();
    public Dictionary<string, int> Weapons { get; } = new();
    public Dictionary<string, int> Armors { get; } = new();

    /// <summary>
    /// Update the game state.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        PlayTime += gameTime.ElapsedGameTime;
    }

    /// <summary>
    /// Get the value of a switch.
    /// </summary>
    public bool GetSwitch(int id)
    {
        return _switches.TryGetValue(id, out bool value) && value;
    }

    /// <summary>
    /// Set the value of a switch.
    /// </summary>
    public void SetSwitch(int id, bool value)
    {
        _switches[id] = value;
    }

    /// <summary>
    /// Export switches for serialization.
    /// </summary>
    public Dictionary<int, bool> ExportSwitches()
    {
        return new Dictionary<int, bool>(_switches);
    }

    /// <summary>
    /// Import switches from serialized data.
    /// </summary>
    public void ImportSwitches(Dictionary<int, bool> switches)
    {
        _switches.Clear();
        foreach (var pair in switches)
        {
            _switches[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// Get the value of a variable.
    /// </summary>
    public int GetVariable(int id)
    {
        return _variables.TryGetValue(id, out int value) ? value : 0;
    }

    /// <summary>
    /// Set the value of a variable.
    /// </summary>
    public void SetVariable(int id, int value)
    {
        _variables[id] = value;
    }

    /// <summary>
    /// Export variables for serialization.
    /// </summary>
    public Dictionary<int, int> ExportVariables()
    {
        return new Dictionary<int, int>(_variables);
    }

    /// <summary>
    /// Import variables from serialized data.
    /// </summary>
    public void ImportVariables(Dictionary<int, int> variables)
    {
        _variables.Clear();
        foreach (var pair in variables)
        {
            _variables[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// Get a self-switch value (event-specific switch).
    /// </summary>
    public bool GetSelfSwitch(string mapId, string eventId, string switchKey)
    {
        string key = $"{mapId}_{eventId}_{switchKey}";
        return _selfSwitches.TryGetValue(key, out bool value) && value;
    }

    /// <summary>
    /// Set a self-switch value.
    /// </summary>
    public void SetSelfSwitch(string mapId, string eventId, string switchKey, bool value)
    {
        string key = $"{mapId}_{eventId}_{switchKey}";
        _selfSwitches[key] = value;
    }

    /// <summary>
    /// Export self switches for serialization.
    /// </summary>
    public Dictionary<string, bool> ExportSelfSwitches()
    {
        return new Dictionary<string, bool>(_selfSwitches);
    }

    /// <summary>
    /// Import self switches from serialized data.
    /// </summary>
    public void ImportSelfSwitches(Dictionary<string, bool> selfSwitches)
    {
        _selfSwitches.Clear();
        foreach (var pair in selfSwitches)
        {
            _selfSwitches[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// Add an item to the inventory.
    /// </summary>
    public void AddItem(string itemId, int count = 1)
    {
        if (Inventory.ContainsKey(itemId))
            Inventory[itemId] += count;
        else
            Inventory[itemId] = count;
    }

    /// <summary>
    /// Remove an item from the inventory.
    /// </summary>
    public void RemoveItem(string itemId, int count = 1)
    {
        if (!Inventory.ContainsKey(itemId))
            return;

        Inventory[itemId] -= count;
        if (Inventory[itemId] <= 0)
            Inventory.Remove(itemId);
    }

    /// <summary>
    /// Check if the player has an item.
    /// </summary>
    public bool HasItem(string itemId, int count = 1)
    {
        return Inventory.TryGetValue(itemId, out int itemCount) && itemCount >= count;
    }

    /// <summary>
    /// Reset all game state.
    /// </summary>
    public void Reset()
    {
        _switches.Clear();
        _variables.Clear();
        _selfSwitches.Clear();
        PartyMembers.Clear();
        Inventory.Clear();
        Weapons.Clear();
        Armors.Clear();
        PartyGold = 0;
        PlayTime = TimeSpan.Zero;
        CurrentMapId = null;
        PlayerX = 0;
        PlayerY = 0;
    }
}
