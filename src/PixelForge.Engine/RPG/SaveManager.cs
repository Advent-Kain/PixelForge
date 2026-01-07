using System.Text.Json;
using PixelForge.Engine.Core;

namespace PixelForge.Engine.RPG;

/// <summary>
/// Manages save and load operations.
/// </summary>
public class SaveManager
{
    private readonly string _savePath;
    private const int MaxSaveSlots = 20;

    public SaveManager(string savePath = "Saves")
    {
        _savePath = savePath;
        Directory.CreateDirectory(_savePath);
    }

    /// <summary>
    /// Save game to slot.
    /// </summary>
    public bool SaveGame(int slot, GameEngine game)
    {
        if (slot < 0 || slot >= MaxSaveSlots)
            return false;

        try
        {
            var saveData = CreateSaveData(slot, game);
            string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string filePath = GetSaveFilePath(slot);
            File.WriteAllText(filePath, json);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Load game from slot.
    /// </summary>
    public bool LoadGame(int slot, GameEngine game)
    {
        if (slot < 0 || slot >= MaxSaveSlots)
            return false;

        try
        {
            string filePath = GetSaveFilePath(slot);
            if (!File.Exists(filePath))
                return false;

            string json = File.ReadAllText(filePath);
            var saveData = JsonSerializer.Deserialize<SaveData>(json);

            if (saveData == null)
                return false;

            ApplySaveData(saveData, game);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get save data for slot without loading.
    /// </summary>
    public SaveData? GetSaveData(int slot)
    {
        if (slot < 0 || slot >= MaxSaveSlots)
            return null;

        try
        {
            string filePath = GetSaveFilePath(slot);
            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SaveData>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Delete save in slot.
    /// </summary>
    public bool DeleteSave(int slot)
    {
        if (slot < 0 || slot >= MaxSaveSlots)
            return false;

        try
        {
            string filePath = GetSaveFilePath(slot);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if save exists.
    /// </summary>
    public bool SaveExists(int slot)
    {
        if (slot < 0 || slot >= MaxSaveSlots)
            return false;

        return File.Exists(GetSaveFilePath(slot));
    }

    /// <summary>
    /// Get all save slots.
    /// </summary>
    public List<SaveSlotInfo> GetAllSaveSlots()
    {
        var slots = new List<SaveSlotInfo>();

        for (int i = 0; i < MaxSaveSlots; i++)
        {
            var saveData = GetSaveData(i);
            slots.Add(new SaveSlotInfo
            {
                Slot = i,
                Exists = saveData != null,
                Data = saveData
            });
        }

        return slots;
    }

    /// <summary>
    /// Create save data from current game state.
    /// </summary>
    private SaveData CreateSaveData(int slot, GameEngine game)
    {
        var gameState = game.GetGameState();
        // TODO: Get party manager and inventory manager from game

        var saveData = new SaveData
        {
            SaveSlot = slot,
            Timestamp = DateTime.Now,
            PlayTime = gameState.PlayTime,
            CurrentMapId = gameState.CurrentMapId,
            Gold = gameState.PartyGold,
            Inventory = new Dictionary<string, int>(gameState.Inventory)
        };

        // Save switches and variables
        // Note: GameState stores these in private dictionaries, would need to expose them

        return saveData;
    }

    /// <summary>
    /// Apply save data to game.
    /// </summary>
    private void ApplySaveData(SaveData saveData, GameEngine game)
    {
        var gameState = game.GetGameState();

        // Clear state that should be fully rehydrated from save data
        gameState.Inventory.Clear();
        gameState.PartyMembers.Clear();

        // Apply basic state
        gameState.CurrentMapId = saveData.CurrentMapId;
        gameState.PartyGold = saveData.Gold;

        // Apply inventory
        foreach (var item in saveData.Inventory)
        {
            gameState.AddItem(item.Key, item.Value);
        }

        // Load map
        if (!string.IsNullOrEmpty(saveData.CurrentMapId))
        {
            game.LoadMap(saveData.CurrentMapId);
        }

        // TODO: Restore party, switches, variables
    }

    /// <summary>
    /// Get file path for save slot.
    /// </summary>
    private string GetSaveFilePath(int slot)
    {
        return Path.Combine(_savePath, $"save{slot:D2}.json");
    }
}

/// <summary>
/// Information about a save slot.
/// </summary>
public class SaveSlotInfo
{
    public int Slot { get; set; }
    public bool Exists { get; set; }
    public SaveData? Data { get; set; }

    public string GetDisplayText()
    {
        if (!Exists || Data == null)
            return $"Slot {Slot + 1}: Empty";

        return $"Slot {Slot + 1}: {Data.GetDisplayString()}";
    }
}
