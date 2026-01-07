using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.RPG;

/// <summary>
/// Shop for buying and selling items.
/// </summary>
public class Shop
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Shop";
    public List<ShopItem> Items { get; set; } = new();
    public List<ShopItem> Weapons { get; set; } = new();
    public List<ShopItem> Armors { get; set; } = new();
    public bool CanSell { get; set; } = true;
    public float SellPriceRate { get; set; } = 0.5f; // 50% of buy price
}

/// <summary>
/// Item available in a shop.
/// </summary>
public class ShopItem
{
    public string ItemId { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public int Price { get; set; }
    public bool Unlimited { get; set; } = true;
    public int Stock { get; set; } = 1;
}

/// <summary>
/// Type of shop item.
/// </summary>
public enum ItemType
{
    Item,
    Weapon,
    Armor
}

/// <summary>
/// Manages shop transactions.
/// </summary>
public class ShopManager
{
    private readonly InventoryManager _inventory;
    private readonly PartyManager _party;

    public ShopManager(InventoryManager inventory, PartyManager party)
    {
        _inventory = inventory;
        _party = party;
    }

    /// <summary>
    /// Buy item from shop.
    /// </summary>
    public bool BuyItem(ShopItem shopItem, string itemId, int quantity)
    {
        int totalPrice = shopItem.Price * quantity;

        // Check if player has enough gold
        if (_party.Gold < totalPrice)
            return false;

        // Check stock
        if (!shopItem.Unlimited && shopItem.Stock < quantity)
            return false;

        // Process purchase
        _party.SpendGold(totalPrice);

        switch (shopItem.ItemType)
        {
            case ItemType.Item:
                _inventory.AddItem(itemId, quantity);
                break;
            case ItemType.Weapon:
                _inventory.AddWeapon(itemId, quantity);
                break;
            case ItemType.Armor:
                _inventory.AddArmor(itemId, quantity);
                break;
        }

        // Update stock
        if (!shopItem.Unlimited)
        {
            shopItem.Stock -= quantity;
        }

        return true;
    }

    /// <summary>
    /// Sell item to shop.
    /// </summary>
    public bool SellItem(ItemType itemType, string itemId, int quantity, int basePrice, float sellRate)
    {
        // Check if player has the item
        bool hasItem = itemType switch
        {
            ItemType.Item => _inventory.HasItem(itemId, quantity),
            ItemType.Weapon => _inventory.GetWeaponCount(itemId) >= quantity,
            ItemType.Armor => _inventory.GetArmorCount(itemId) >= quantity,
            _ => false
        };

        if (!hasItem)
            return false;

        // Calculate sell price
        int sellPrice = (int)(basePrice * sellRate * quantity);

        // Remove from inventory
        bool removed = itemType switch
        {
            ItemType.Item => _inventory.RemoveItem(itemId, quantity),
            ItemType.Weapon => _inventory.RemoveWeapon(itemId, quantity),
            ItemType.Armor => _inventory.RemoveArmor(itemId, quantity),
            _ => false
        };

        if (removed)
        {
            _party.GainGold(sellPrice);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate sell price for an item.
    /// </summary>
    public int GetSellPrice(int basePrice, float sellRate)
    {
        return (int)(basePrice * sellRate);
    }
}
