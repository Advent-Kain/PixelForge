using PixelForge.Engine.Core;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Battle;

public static class BattleItemEffects
{
    public static void ApplyItem(GameEngine game, BattleAction action)
    {
        if (action.Item == null || action.Targets.Count == 0)
            return;

        foreach (var target in action.Targets)
        {
            if (!target.IsAlive && action.Item.Scope is not SkillScope.OneAllyDead and not SkillScope.AllAlliesDead)
                continue;

            foreach (var effect in action.Item.Effects)
            {
                ApplyEffect(game, target, effect);
            }
        }

        if (action.Item.Consumable && !string.IsNullOrWhiteSpace(action.ItemId))
        {
            game.GetInventoryManager().RemoveItem(action.ItemId, 1);
        }
    }

    private static void ApplyEffect(GameEngine game, Battler target, Effect effect)
    {
        switch (effect.Code)
        {
            case EffectCode.RecoverHp:
                ApplyHpRecovery(game, target, effect);
                break;
            case EffectCode.RecoverMp:
                ApplyMpRecovery(game, target, effect);
                break;
            case EffectCode.GainTp:
                target.CurrentTp += (int)effect.Value1;
                break;
            case EffectCode.RemoveState:
                if (!string.IsNullOrEmpty(effect.DataId))
                {
                    target.States.RemoveAll(s => s.Id == effect.DataId);
                }
                break;
        }
    }

    private static void ApplyHpRecovery(GameEngine game, Battler target, Effect effect)
    {
        var stats = BattleFormulaEvaluator.GetStats(game, target);
        int healAmount = (int)(effect.Value1 + stats.MaxHp * effect.Value2 / 100f);
        int maxHp = stats.MaxHp;
        target.CurrentHp = Math.Min(maxHp, target.CurrentHp + Math.Max(0, healAmount));
    }

    private static void ApplyMpRecovery(GameEngine game, Battler target, Effect effect)
    {
        var stats = BattleFormulaEvaluator.GetStats(game, target);
        int recoverAmount = (int)(effect.Value1 + stats.MaxMp * effect.Value2 / 100f);
        int maxMp = stats.MaxMp;
        target.CurrentMp = Math.Min(maxMp, target.CurrentMp + Math.Max(0, recoverAmount));
    }
}
