using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.RPG;

/// <summary>
/// Manages the game inventory.
/// </summary>
public class InventoryManager
{
    private readonly Dictionary<string, int> _items = new();
    private readonly Dictionary<string, int> _weapons = new();
    private readonly Dictionary<string, int> _armors = new();
    private readonly int _maxItemCount = 99;

    /// <summary>
    /// Add item to inventory.
    /// </summary>
    public bool AddItem(string itemId, int count = 1)
    {
        if (_items.ContainsKey(itemId))
        {
            int newCount = _items[itemId] + count;
            if (newCount > _maxItemCount)
                return false;

            _items[itemId] = newCount;
        }
        else
        {
            if (count > _maxItemCount)
                return false;

            _items[itemId] = count;
        }

        return true;
    }

    /// <summary>
    /// Remove item from inventory.
    /// </summary>
    public bool RemoveItem(string itemId, int count = 1)
    {
        if (!_items.ContainsKey(itemId))
            return false;

        _items[itemId] -= count;

        if (_items[itemId] <= 0)
        {
            _items.Remove(itemId);
        }

        return true;
    }

    /// <summary>
    /// Get item count.
    /// </summary>
    public int GetItemCount(string itemId)
    {
        return _items.TryGetValue(itemId, out int count) ? count : 0;
    }

    /// <summary>
    /// Check if has item.
    /// </summary>
    public bool HasItem(string itemId, int count = 1)
    {
        return GetItemCount(itemId) >= count;
    }

    /// <summary>
    /// Get all items.
    /// </summary>
    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(_items);
    }

    /// <summary>
    /// Add weapon.
    /// </summary>
    public bool AddWeapon(string weaponId, int count = 1)
    {
        if (_weapons.ContainsKey(weaponId))
        {
            int newCount = _weapons[weaponId] + count;
            if (newCount > _maxItemCount)
                return false;

            _weapons[weaponId] = newCount;
        }
        else
        {
            if (count > _maxItemCount)
                return false;

            _weapons[weaponId] = count;
        }

        return true;
    }

    /// <summary>
    /// Remove weapon.
    /// </summary>
    public bool RemoveWeapon(string weaponId, int count = 1)
    {
        if (!_weapons.ContainsKey(weaponId))
            return false;

        _weapons[weaponId] -= count;

        if (_weapons[weaponId] <= 0)
        {
            _weapons.Remove(weaponId);
        }

        return true;
    }

    /// <summary>
    /// Get weapon count.
    /// </summary>
    public int GetWeaponCount(string weaponId)
    {
        return _weapons.TryGetValue(weaponId, out int count) ? count : 0;
    }

    /// <summary>
    /// Get all weapons.
    /// </summary>
    public Dictionary<string, int> GetAllWeapons()
    {
        return new Dictionary<string, int>(_weapons);
    }

    /// <summary>
    /// Add armor.
    /// </summary>
    public bool AddArmor(string armorId, int count = 1)
    {
        if (_armors.ContainsKey(armorId))
        {
            int newCount = _armors[armorId] + count;
            if (newCount > _maxItemCount)
                return false;

            _armors[armorId] = newCount;
        }
        else
        {
            if (count > _maxItemCount)
                return false;

            _armors[armorId] = count;
        }

        return true;
    }

    /// <summary>
    /// Remove armor.
    /// </summary>
    public bool RemoveArmor(string armorId, int count = 1)
    {
        if (!_armors.ContainsKey(armorId))
            return false;

        _armors[armorId] -= count;

        if (_armors[armorId] <= 0)
        {
            _armors.Remove(armorId);
        }

        return true;
    }

    /// <summary>
    /// Get armor count.
    /// </summary>
    public int GetArmorCount(string armorId)
    {
        return _armors.TryGetValue(armorId, out int count) ? count : 0;
    }

    /// <summary>
    /// Get all armors.
    /// </summary>
    public Dictionary<string, int> GetAllArmors()
    {
        return new Dictionary<string, int>(_armors);
    }

    /// <summary>
    /// Use an item on a target.
    /// </summary>
    public bool UseItem(string itemId, Item item, GameActor target)
    {
        if (!HasItem(itemId))
            return false;

        // Apply effects
        foreach (var effect in item.Effects)
        {
            ApplyEffect(effect, target);
        }

        // Remove if consumable
        if (item.Consumable)
        {
            RemoveItem(itemId, 1);
        }

        return true;
    }

    /// <summary>
    /// Apply item effect to actor.
    /// </summary>
    private void ApplyEffect(Effect effect, GameActor target)
    {
        switch (effect.Code)
        {
            case EffectCode.RecoverHp:
                int hpRecover = (int)(effect.Value1 + target.GetCurrentStats().MaxHp * effect.Value2 / 100);
                target.HealHp(hpRecover);
                break;

            case EffectCode.RecoverMp:
                int mpRecover = (int)(effect.Value1 + target.GetCurrentStats().MaxMp * effect.Value2 / 100);
                target.HealMp(mpRecover);
                break;

            case EffectCode.GainTp:
                target.CurrentTp += (int)effect.Value1;
                break;

            case EffectCode.RemoveState:
                if (!string.IsNullOrEmpty(effect.DataId))
                {
                    target.States.RemoveAll(s => s.StateData.Id == effect.DataId);
                }
                break;

            // TODO: Implement other effect types
        }
    }

    /// <summary>
    /// Sort inventory.
    /// </summary>
    public void SortItems()
    {
        // Items are stored in dictionary, sorting handled by UI
    }
}
