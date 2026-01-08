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
        var partyManager = game.GetPartyManager();
        var inventoryManager = game.GetInventoryManager();

        var saveData = new SaveData
        {
            SaveSlot = slot,
            Timestamp = DateTime.Now,
            PlayTime = gameState.PlayTime,
            CurrentMapId = gameState.CurrentMapId,
            PlayerX = gameState.PlayerX,
            PlayerY = gameState.PlayerY,
            Gold = partyManager.Gold,
            Party = partyManager.Party
                .Select(actor => new SavedActor
                {
                    ActorId = actor.ActorId,
                    Name = actor.ActorData?.Name ?? actor.ActorId,
                    Level = actor.Level,
                    Experience = actor.Experience,
                    CurrentHp = actor.CurrentHp,
                    CurrentMp = actor.CurrentMp,
                    CurrentTp = actor.CurrentTp,
                    Equipment = new Dictionary<string, string?>(actor.EquippedItems),
                    LearnedSkills = new List<string>(actor.LearnedSkills),
                    States = actor.States
                        .Select(state => new SavedState
                        {
                            StateId = state.StateData.Id,
                            TurnsRemaining = state.TurnsRemaining
                        })
                        .ToList()
                })
                .ToList(),
            Inventory = inventoryManager.GetAllItems(),
            Weapons = inventoryManager.GetAllWeapons(),
            Armors = inventoryManager.GetAllArmors(),
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
        var partyManager = game.GetPartyManager();
        var inventoryManager = game.GetInventoryManager();

        // Clear state that should be fully rehydrated from save data
        gameState.Inventory.Clear();
        gameState.PartyMembers.Clear();
        gameState.Weapons.Clear();
        gameState.Armors.Clear();

        // Apply basic state
        gameState.CurrentMapId = saveData.CurrentMapId;
        gameState.PlayerX = saveData.PlayerX;
        gameState.PlayerY = saveData.PlayerY;
        gameState.PartyGold = saveData.Gold;

        var existingActors = partyManager.Party
            .Where(actor => !string.IsNullOrEmpty(actor.ActorId))
            .ToDictionary(actor => actor.ActorId, actor => actor);

        partyManager.Clear();
        partyManager.Gold = saveData.Gold;

        // Apply inventory
        inventoryManager.Clear();
        foreach (var item in saveData.Inventory)
        {
            inventoryManager.AddItem(item.Key, item.Value);
        }

        foreach (var weapon in saveData.Weapons)
        {
            inventoryManager.AddWeapon(weapon.Key, weapon.Value);
        }

        foreach (var armor in saveData.Armors)
        {
            inventoryManager.AddArmor(armor.Key, armor.Value);
        }

        foreach (var member in saveData.Party)
        {
            if (string.IsNullOrEmpty(member.ActorId))
            {
                continue;
            }

            if (!existingActors.TryGetValue(member.ActorId, out var actor))
            {
                actor = new GameActor
                {
                    ActorId = member.ActorId,
                    ActorData = new Shared.Models.Database.Actor
                    {
                        Id = member.ActorId,
                        Name = member.Name
                    }
                };
            }

            actor.Level = member.Level;
            actor.Experience = member.Experience;
            actor.CurrentHp = member.CurrentHp;
            actor.CurrentMp = member.CurrentMp;
            actor.CurrentTp = member.CurrentTp;
            actor.EquippedItems = new Dictionary<string, string?>(member.Equipment);
            actor.LearnedSkills = new List<string>(member.LearnedSkills);
            actor.States = member.States
                .Select(state => new ActiveState
                {
                    StateData = new Shared.Models.Database.State
                    {
                        Id = state.StateId
                    },
                    TurnsRemaining = state.TurnsRemaining
                })
                .ToList();

            partyManager.AddActor(actor);
        }

        gameState.PartyMembers.Clear();
        foreach (var actor in partyManager.Party)
        {
            gameState.PartyMembers.Add(actor.ActorId);
        }

        gameState.Inventory.Clear();
        foreach (var item in inventoryManager.GetAllItems())
        {
            gameState.AddItem(item.Key, item.Value);
        }

        gameState.Weapons.Clear();
        foreach (var weapon in inventoryManager.GetAllWeapons())
        {
            gameState.Weapons[weapon.Key] = weapon.Value;
        }

        gameState.Armors.Clear();
        foreach (var armor in inventoryManager.GetAllArmors())
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
