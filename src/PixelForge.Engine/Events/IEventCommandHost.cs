namespace PixelForge.Engine.Events;

public interface IEventCommandHost
{
    void StartBattle(string troopId, bool canEscape, bool canLose);
    void OpenShop(IReadOnlyList<ShopGood> goods, bool purchaseOnly);
    void ExecuteScript(string script);
}

public record ShopGood(int Type, string ItemId, int PriceType, int Price);
