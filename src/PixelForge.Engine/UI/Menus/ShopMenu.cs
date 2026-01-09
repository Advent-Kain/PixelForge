using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.UI;

/// <summary>
/// Shop menu for buying and selling items.
/// </summary>
public class ShopMenu : IMenu
{
    private readonly GameEngine _game;
    private readonly ShopManager _shopManager;
    private Shop? _currentShop;
    private ShopMode _mode = ShopMode.Buy;
    private int _selectedIndex;
    private int _scrollOffset;
    private readonly int _visibleItems = 8;
    private int _quantity = 1;

    private KeyboardState _previousKeyboard;

    public ShopMenu(GameEngine game, ShopManager shopManager)
    {
        _game = game;
        _shopManager = shopManager;
    }

    public void OpenShop(Shop shop)
    {
        _currentShop = shop;
        _mode = ShopMode.Buy;
        _selectedIndex = 0;
        _scrollOffset = 0;
        _quantity = 1;
    }

    public void OnOpen()
    {
        _selectedIndex = 0;
        _scrollOffset = 0;
        _quantity = 1;
    }

    public void OnClose()
    {
        _currentShop = null;
    }

    public void Update(GameTime gameTime)
    {
        if (_currentShop == null)
            return;

        var keyboard = Keyboard.GetState();

        // Switch mode (Tab key)
        if (keyboard.IsKeyDown(Keys.Tab) && _previousKeyboard.IsKeyUp(Keys.Tab))
        {
            _mode = _mode == ShopMode.Buy ? ShopMode.Sell : ShopMode.Buy;
            _selectedIndex = 0;
            _scrollOffset = 0;
            _quantity = 1;
        }

        var items = GetCurrentItems();
        if (items.Count == 0)
        {
            _previousKeyboard = keyboard;
            return;
        }

        // Navigate
        if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
        {
            _selectedIndex = Math.Min(_selectedIndex + 1, items.Count - 1);
            if (_selectedIndex >= _scrollOffset + _visibleItems)
            {
                _scrollOffset++;
            }
        }
        else if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
        {
            _selectedIndex = Math.Max(_selectedIndex - 1, 0);
            if (_selectedIndex < _scrollOffset)
            {
                _scrollOffset--;
            }
        }

        // Adjust quantity
        if (keyboard.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))
        {
            _quantity = Math.Min(_quantity + 1, 99);
        }
        else if (keyboard.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))
        {
            _quantity = Math.Max(_quantity - 1, 1);
        }

        // Confirm transaction
        if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
        {
            if (_mode == ShopMode.Buy)
                PerformBuy();
            else
                PerformSell();
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixelTexture)
    {
        if (_currentShop == null)
            return;

        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Draw background
        spriteBatch.Draw(pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * 0.7f);

        // Draw shop window
        var windowRect = new Rectangle(50, 50, viewport.Width - 100, viewport.Height - 100);
        spriteBatch.Draw(pixelTexture, windowRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, pixelTexture, windowRect, Color.White, 2);

        // Draw title and mode
        Vector2 titlePos = new Vector2(windowRect.X + 20, windowRect.Y + 10);
        string title = $"{_currentShop.Name} - {(_mode == ShopMode.Buy ? "Buy" : "Sell")}";
        spriteBatch.DrawString(font, title, titlePos, Color.White);

        // Draw gold
        string goldText = $"Gold: {_game.GetGameState().PartyGold}";
        Vector2 goldPos = new Vector2(windowRect.Right - 200, windowRect.Y + 10);
        spriteBatch.DrawString(font, goldText, goldPos, Color.Yellow);

        // Draw items
        var items = GetCurrentItems();
        if (items.Count == 0)
        {
            Vector2 emptyPos = new Vector2(windowRect.X + 40, windowRect.Y + 60);
            string emptyText = _mode == ShopMode.Buy ? "No items for sale" : "No items to sell";
            spriteBatch.DrawString(font, emptyText, emptyPos, Color.Gray);
        }
        else
        {
            int startIndex = _scrollOffset;
            int endIndex = Math.Min(_scrollOffset + _visibleItems, items.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                var item = items[i];
                int displayIndex = i - _scrollOffset;
                Color color = i == _selectedIndex ? Color.Yellow : Color.White;

                Vector2 pos = new Vector2(
                    windowRect.X + 40,
                    windowRect.Y + 60 + displayIndex * 35
                );

                if (i == _selectedIndex)
                {
                    spriteBatch.DrawString(font, ">", pos - new Vector2(20, 0), Color.Yellow);
                }

                string itemText = _mode == ShopMode.Buy
                    ? $"{item.Name} - {item.Price}G"
                    : $"{item.Name} x{item.Count} - {item.Price}G";
                spriteBatch.DrawString(font, itemText, pos, color);
            }
        }

        // Draw transaction panel
        if (items.Count > 0 && _selectedIndex >= 0 && _selectedIndex < items.Count)
        {
            var transactionRect = new Rectangle(
                windowRect.X + 20,
                windowRect.Bottom - 150,
                windowRect.Width - 40,
                130
            );

            spriteBatch.Draw(pixelTexture, transactionRect, Color.Black * 0.5f);
            DrawBorder(spriteBatch, pixelTexture, transactionRect, Color.Gray, 1);

            var selectedItem = items[_selectedIndex];
            Vector2 transPos = new Vector2(transactionRect.X + 10, transactionRect.Y + 10);

            spriteBatch.DrawString(font, selectedItem.Description ?? "No description.", transPos, Color.White);

            // Quantity selector
            Vector2 qtyPos = new Vector2(transactionRect.X + 10, transactionRect.Y + 50);
            spriteBatch.DrawString(font, $"Quantity: < {_quantity} >", qtyPos, Color.Yellow);

            // Total price
            int totalPrice = selectedItem.Price * _quantity;
            Vector2 totalPos = new Vector2(transactionRect.X + 10, transactionRect.Y + 80);
            spriteBatch.DrawString(font, $"Total: {totalPrice}G", totalPos, Color.Cyan);

            // Instructions
            Vector2 instructPos = new Vector2(transactionRect.X + 10, transactionRect.Bottom - 25);
            string instructions = "Tab: Switch Mode | ←→: Quantity | Enter: Confirm | Esc: Exit";
            spriteBatch.DrawString(font, instructions, instructPos, Color.Gray);
        }
    }

    private List<ShopItemInfo> GetCurrentItems()
    {
        if (_currentShop == null)
            return new List<ShopItemInfo>();

        if (_mode == ShopMode.Buy)
        {
            var items = new List<ShopItemInfo>();
            AddShopItems(items, _currentShop.Items);
            AddShopItems(items, _currentShop.Weapons);
            AddShopItems(items, _currentShop.Armors);
            return items;
        }
        else
        {
            var items = new List<ShopItemInfo>();
            var inventory = _game.GetInventoryManager();

            AddSellItems(items, RPG.ItemType.Item, inventory.GetAllItems());
            AddSellItems(items, RPG.ItemType.Weapon, inventory.GetAllWeapons());
            AddSellItems(items, RPG.ItemType.Armor, inventory.GetAllArmors());

            return items;
        }
    }

    private void PerformBuy()
    {
        if (_currentShop == null || _selectedIndex < 0)
            return;

        var items = GetCurrentItems();
        if (_selectedIndex >= items.Count)
            return;

        var item = items[_selectedIndex];
        var shopItem = FindShopItem(item);

        if (shopItem != null)
        {
            if (_shopManager.BuyItem(shopItem, item.Id, _quantity, item.BasePrice))
            {
                // TODO: Show success message
                _quantity = 1;
            }
            else
            {
                // TODO: Show error message
            }
        }
    }

    private void PerformSell()
    {
        if (_currentShop == null || _selectedIndex < 0)
            return;

        var items = GetCurrentItems();
        if (_selectedIndex >= items.Count)
            return;

        var item = items[_selectedIndex];

        if (_shopManager.SellItem(item.ItemType, item.Id, _quantity, item.BasePrice, _currentShop.SellPriceRate))
        {
            // TODO: Show success message
            _quantity = 1;
        }
        else
        {
            // TODO: Show error message
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(texture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void AddShopItems(List<ShopItemInfo> items, IEnumerable<ShopItem> shopItems)
    {
        foreach (var shopItem in shopItems)
        {
            var itemData = GetItemData(shopItem.ItemType, shopItem.ItemId);
            int basePrice = shopItem.Price > 0 ? shopItem.Price : itemData?.Price ?? 0;

            items.Add(new ShopItemInfo
            {
                Id = shopItem.ItemId,
                ItemType = shopItem.ItemType,
                Name = itemData?.Name ?? shopItem.ItemId,
                Description = itemData?.Description,
                Price = basePrice,
                BasePrice = basePrice,
                Count = shopItem.Unlimited ? 999 : shopItem.Stock
            });
        }
    }

    private void AddSellItems(List<ShopItemInfo> items, RPG.ItemType itemType, Dictionary<string, int> inventoryItems)
    {
        foreach (var item in inventoryItems)
        {
            var itemData = GetItemData(itemType, item.Key);
            int basePrice = itemData?.Price ?? 0;
            int sellPrice = _shopManager.GetSellPrice(basePrice, _currentShop?.SellPriceRate ?? 0f);

            items.Add(new ShopItemInfo
            {
                Id = item.Key,
                ItemType = itemType,
                Name = itemData?.Name ?? item.Key,
                Description = itemData?.Description,
                Price = sellPrice,
                BasePrice = basePrice,
                Count = item.Value
            });
        }
    }

    private ItemBase? GetItemData(RPG.ItemType itemType, string itemId)
    {
        var database = _game.GetDatabase();
        return itemType switch
        {
            RPG.ItemType.Item => database.GetItem(itemId),
            RPG.ItemType.Weapon => database.GetWeapon(itemId),
            RPG.ItemType.Armor => database.GetArmor(itemId),
            _ => null
        };
    }

    private ShopItem? FindShopItem(ShopItemInfo item)
    {
        if (_currentShop == null)
            return null;

        return item.ItemType switch
        {
            RPG.ItemType.Item => _currentShop.Items.FirstOrDefault(si => si.ItemId == item.Id),
            RPG.ItemType.Weapon => _currentShop.Weapons.FirstOrDefault(si => si.ItemId == item.Id),
            RPG.ItemType.Armor => _currentShop.Armors.FirstOrDefault(si => si.ItemId == item.Id),
            _ => null
        };
    }

    private class ShopItemInfo
    {
        public string Id { get; set; } = string.Empty;
        public RPG.ItemType ItemType { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Price { get; set; }
        public int BasePrice { get; set; }
        public int Count { get; set; }
    }
}

public enum ShopMode
{
    Buy,
    Sell
}
