using PixelForge.Engine.Core;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Battle;

public record BattleItemOption(string Id, Item Item, int Count);

public static class BattleSelectionHelper
{
    public static List<Skill> GetAvailableSkills(GameEngine game, Battler battler)
    {
        if (!battler.IsActor)
            return new List<Skill>();

        var partyActor = game.GetPartyManager().GetActor(battler.ActorId ?? string.Empty);
        if (partyActor == null)
            return new List<Skill>();

        var database = game.GetDatabase();
        return partyActor.LearnedSkills
            .Select(database.GetSkill)
            .Where(skill => skill != null && IsBattleUsableSkill(skill))
            .Select(skill => skill!)
            .OrderBy(skill => skill.SkillType)
            .ThenBy(skill => skill.Name)
            .ToList();
    }

    public static List<BattleItemOption> GetAvailableItems(GameEngine game)
    {
        var inventory = game.GetInventoryManager();
        var database = game.GetDatabase();
        var options = new List<BattleItemOption>();

        foreach (var (itemId, count) in inventory.GetAllItems())
        {
            if (count <= 0)
                continue;

            var item = database.GetItem(itemId);
            if (item == null || !IsBattleUsableItem(item))
                continue;

            options.Add(new BattleItemOption(itemId, item, count));
        }

        return options
            .OrderBy(option => option.Item.Name)
            .ToList();
    }

    private static bool IsBattleUsableSkill(Skill skill)
    {
        return skill.Occasion is ItemOccasion.Always or ItemOccasion.BattleOnly;
    }

    private static bool IsBattleUsableItem(Item item)
    {
        return item.Occasion is ItemOccasion.Always or ItemOccasion.BattleOnly;
    }
}
