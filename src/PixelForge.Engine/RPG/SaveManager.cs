using System.Text.Json;
using System.Linq;
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
            PlayerX = gameState.PlayerX,
            PlayerY = gameState.PlayerY,
            Gold = gameState.PartyGold,
            Party = gameState.PartyMembers
                .Select(memberId => new SavedActor
                {
                    ActorId = memberId,
                    Name = memberId,
                    Level = 1
                })
                .ToList(),
            Inventory = new Dictionary<string, int>(gameState.Inventory),
            Weapons = new Dictionary<string, int>(gameState.Weapons),
            Armors = new Dictionary<string, int>(gameState.Armors),
            Switches = gameState.ExportSwitches(),
            Variables = gameState.ExportVariables(),
            SelfSwitches = gameState.ExportSelfSwitches()
        };

        return saveData;
    }

    /// <summary>
    /// Apply save data to game.
    /// </summary>
    private void ApplySaveData(SaveData saveData, GameEngine game)
    {
        var gameState = game.GetGameState();

        // Apply basic state
        gameState.CurrentMapId = saveData.CurrentMapId;
        gameState.PlayerX = saveData.PlayerX;
        gameState.PlayerY = saveData.PlayerY;
        gameState.PartyGold = saveData.Gold;

        gameState.PartyMembers.Clear();
        foreach (var member in saveData.Party)
        {
            if (!string.IsNullOrEmpty(member.ActorId))
            {
                gameState.PartyMembers.Add(member.ActorId);
            }
        }

        // Apply inventory
        gameState.Inventory.Clear();
        foreach (var item in saveData.Inventory)
        {
            gameState.AddItem(item.Key, item.Value);
        }

        gameState.Weapons.Clear();
        foreach (var weapon in saveData.Weapons)
        {
            gameState.Weapons[weapon.Key] = weapon.Value;
        }

        gameState.Armors.Clear();
        foreach (var armor in saveData.Armors)
        {
            gameState.Armors[armor.Key] = armor.Value;
        }

        gameState.ImportSwitches(saveData.Switches);
        gameState.ImportVariables(saveData.Variables);
        gameState.ImportSelfSwitches(saveData.SelfSwitches);

        // Load map
        if (!string.IsNullOrEmpty(saveData.CurrentMapId))
        {
            game.LoadMap(saveData.CurrentMapId);
        }
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
