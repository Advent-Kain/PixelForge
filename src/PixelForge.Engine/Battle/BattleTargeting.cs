using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Battle;

public static class BattleTargeting
{
    private static readonly Random Random = new();

    public static bool RequiresTargetSelection(SkillScope scope)
    {
        return scope is SkillScope.OneEnemy or SkillScope.OneAlly or SkillScope.OneAllyDead;
    }

    public static List<Battler> GetSelectableTargets(BattleState state, Battler user, SkillScope scope)
    {
        return scope switch
        {
            SkillScope.OneEnemy => state.Enemies.Where(b => b.IsAlive).ToList(),
            SkillScope.OneAlly => state.Party.Where(b => b.IsAlive).ToList(),
            SkillScope.OneAllyDead => state.Party.Where(b => !b.IsAlive).ToList(),
            SkillScope.User => new List<Battler> { user },
            _ => new List<Battler>()
        };
    }

    public static List<Battler> SelectTargets(BattleState state, Battler user, SkillScope scope, int selectedIndex)
    {
        switch (scope)
        {
            case SkillScope.OneEnemy:
            case SkillScope.OneAlly:
            case SkillScope.OneAllyDead:
                return SelectSingleTarget(GetSelectableTargets(state, user, scope), selectedIndex);
            case SkillScope.AllEnemies:
                return state.Enemies.Where(b => b.IsAlive).ToList();
            case SkillScope.AllAllies:
                return state.Party.Where(b => b.IsAlive).ToList();
            case SkillScope.AllAlliesDead:
                return state.Party.Where(b => !b.IsAlive).ToList();
            case SkillScope.RandomEnemies:
                return SelectRandomTargets(state.Enemies.Where(b => b.IsAlive).ToList(), 1);
            case SkillScope.TwoRandomEnemies:
                return SelectRandomTargets(state.Enemies.Where(b => b.IsAlive).ToList(), 2);
            case SkillScope.ThreeRandomEnemies:
                return SelectRandomTargets(state.Enemies.Where(b => b.IsAlive).ToList(), 3);
            case SkillScope.FourRandomEnemies:
                return SelectRandomTargets(state.Enemies.Where(b => b.IsAlive).ToList(), 4);
            case SkillScope.User:
                return new List<Battler> { user };
            default:
                return new List<Battler>();
        }
    }

    private static List<Battler> SelectSingleTarget(List<Battler> targets, int selectedIndex)
    {
        if (targets.Count == 0)
            return new List<Battler>();

        int index = Math.Clamp(selectedIndex, 0, targets.Count - 1);
        return new List<Battler> { targets[index] };
    }

    private static List<Battler> SelectRandomTargets(List<Battler> targets, int count)
    {
        if (targets.Count == 0)
            return new List<Battler>();

        return targets
            .OrderBy(_ => Random.Next())
            .Take(Math.Min(count, targets.Count))
            .ToList();
    }
}
