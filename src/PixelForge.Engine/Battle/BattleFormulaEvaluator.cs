using System.Globalization;
using PixelForge.Engine.Core;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Battle;

internal static class BattleFormulaEvaluator
{
    private const string DefaultAttackFormula = "a.atk * 4 - b.def * 2";
    private const float DefaultAttackHitRate = 95f;
    private const float DefaultAttackCritRate = 5f;
    private const float CriticalMultiplier = 1.5f;
    private const float GuardDamageMultiplier = 0.5f;
    private const int DefaultAttackVariance = 10;
    private static readonly Random Random = new();

    public static int CalculateAttackDamage(GameEngine game, Battler attacker, Battler defender)
    {
        return CalculateDamage(game, attacker, defender, DefaultAttackFormula, DefaultAttackHitRate,
            DefaultAttackCritRate, DefaultAttackVariance, true);
    }

    public static int CalculateSkillDamage(GameEngine game, Battler user, Battler target, Skill skill)
    {
        string formula = string.IsNullOrWhiteSpace(skill.Damage?.Formula)
            ? DefaultAttackFormula
            : skill.Damage.Formula;
        int variance = skill.Damage?.Variance > 0 ? skill.Damage.Variance : skill.Variance;
        bool allowCritical = skill.Damage?.Critical ?? true;
        float critRate = allowCritical ? skill.CriticalRate : 0f;
        return CalculateDamage(game, user, target, formula, skill.HitRate, critRate, variance, allowCritical);
    }

    public static int GetMaxHp(GameEngine game, Battler battler)
    {
        return GetStats(game, battler).MaxHp;
    }

    public static float GetAgilityMultiplier(GameEngine game, Battler battler)
    {
        int agility = GetStats(game, battler).Agility;
        return 0.5f + (agility / 100f);
    }

    private static int CalculateDamage(GameEngine game, Battler attacker, Battler defender, string formula,
        float hitRate, float critRate, int variance, bool allowCritical)
    {
        int baseDamage = (int)Math.Round(EvaluateFormula(game, formula, attacker, defender));
        if (baseDamage == 0)
            return 0;

        bool isHealing = baseDamage < 0;
        if (!isHealing && hitRate < 100f && !RollPercent(hitRate))
            return 0;

        if (!isHealing && allowCritical && critRate > 0f && RollPercent(critRate))
        {
            baseDamage = (int)Math.Round(baseDamage * CriticalMultiplier);
        }

        baseDamage = ApplyVariance(baseDamage, variance);

        if (!isHealing && defender.IsGuarding)
        {
            baseDamage = (int)Math.Round(baseDamage * GuardDamageMultiplier);
        }

        return baseDamage;
    }

    private static double EvaluateFormula(GameEngine game, string formula, Battler attacker, Battler defender)
    {
        try
        {
            var variables = BuildFormulaVariables(game, attacker, defender);
            var evaluator = new FormulaEvaluator(formula, variables);
            return evaluator.Evaluate();
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
        catch (FormatException)
        {
            return 0;
        }
    }

    private static Dictionary<string, double> BuildFormulaVariables(GameEngine game, Battler attacker, Battler defender)
    {
        var attackerStats = GetStats(game, attacker);
        var defenderStats = GetStats(game, defender);

        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.atk"] = attackerStats.Attack,
            ["a.def"] = attackerStats.Defense,
            ["a.mat"] = attackerStats.MagicAttack,
            ["a.mdf"] = attackerStats.MagicDefense,
            ["a.agi"] = attackerStats.Agility,
            ["a.luk"] = attackerStats.Luck,
            ["a.hp"] = attacker.CurrentHp,
            ["a.mhp"] = attackerStats.MaxHp,
            ["a.mp"] = attacker.CurrentMp,
            ["a.mmp"] = attackerStats.MaxMp,
            ["b.atk"] = defenderStats.Attack,
            ["b.def"] = defenderStats.Defense,
            ["b.mat"] = defenderStats.MagicAttack,
            ["b.mdf"] = defenderStats.MagicDefense,
            ["b.agi"] = defenderStats.Agility,
            ["b.luk"] = defenderStats.Luck,
            ["b.hp"] = defender.CurrentHp,
            ["b.mhp"] = defenderStats.MaxHp,
            ["b.mp"] = defender.CurrentMp,
            ["b.mmp"] = defenderStats.MaxMp
        };
    }

    public static ActorStats GetStats(GameEngine game, Battler battler)
    {
        var database = game.GetDatabase();
        if (battler.IsActor)
        {
            var partyActor = game.GetPartyManager().GetActor(battler.ActorId ?? string.Empty);
            var actorData = partyActor?.ActorData ?? database.GetActor(battler.ActorId ?? string.Empty);
            return partyActor?.GetCurrentStats() ?? actorData?.BaseStats ?? new ActorStats();
        }

        var enemyData = database.GetEnemy(battler.EnemyId ?? string.Empty);
        return enemyData?.Stats ?? new ActorStats();
    }

    private static int ApplyVariance(int value, int variance)
    {
        if (variance <= 0)
            return value;

        int magnitude = Math.Abs(value);
        int swing = (int)Math.Round(magnitude * (variance / 100f));
        int roll = Random.Next(-swing, swing + 1);
        int varied = magnitude + roll;
        return value < 0 ? -varied : varied;
    }

    private static bool RollPercent(float percent)
    {
        return Random.NextDouble() * 100f < percent;
    }
}

internal sealed class FormulaEvaluator
{
    private readonly string _formula;
    private readonly IReadOnlyDictionary<string, double> _variables;
    private int _position;

    public FormulaEvaluator(string formula, IReadOnlyDictionary<string, double> variables)
    {
        _formula = formula;
        _variables = variables;
    }

    public double Evaluate()
    {
        _position = 0;
        double value = ParseExpression();
        SkipWhitespace();
        if (_position < _formula.Length)
            throw new InvalidOperationException("Unexpected token.");
        return value;
    }

    private double ParseExpression()
    {
        double value = ParseTerm();
        while (true)
        {
            SkipWhitespace();
            if (Match('+'))
            {
                value += ParseTerm();
            }
            else if (Match('-'))
            {
                value -= ParseTerm();
            }
            else
            {
                break;
            }
        }

        return value;
    }

    private double ParseTerm()
    {
        double value = ParseFactor();
        while (true)
        {
            SkipWhitespace();
            if (Match('*'))
            {
                value *= ParseFactor();
            }
            else if (Match('/'))
            {
                value /= ParseFactor();
            }
            else
            {
                break;
            }
        }

        return value;
    }

    private double ParseFactor()
    {
        SkipWhitespace();

        if (Match('+'))
        {
            return ParseFactor();
        }

        if (Match('-'))
        {
            return -ParseFactor();
        }

        if (Match('('))
        {
            double value = ParseExpression();
            SkipWhitespace();
            if (!Match(')'))
                throw new InvalidOperationException("Missing closing parenthesis.");
            return value;
        }

        if (char.IsDigit(Peek()) || Peek() == '.')
        {
            return ParseNumber();
        }

        return ParseVariable();
    }

    private double ParseNumber()
    {
        int start = _position;
        while (_position < _formula.Length &&
               (char.IsDigit(_formula[_position]) || _formula[_position] == '.'))
        {
            _position++;
        }

        string token = _formula[start.._position];
        return double.Parse(token, CultureInfo.InvariantCulture);
    }

    private double ParseVariable()
    {
        int start = _position;
        while (_position < _formula.Length &&
               (char.IsLetterOrDigit(_formula[_position]) || _formula[_position] == '.' || _formula[_position] == '_'))
        {
            _position++;
        }

        string token = _formula[start.._position];
        if (_variables.TryGetValue(token, out double value))
            return value;

        return 0;
    }

    private void SkipWhitespace()
    {
        while (_position < _formula.Length && char.IsWhiteSpace(_formula[_position]))
        {
            _position++;
        }
    }

    private bool Match(char expected)
    {
        if (_position < _formula.Length && _formula[_position] == expected)
        {
            _position++;
            return true;
        }

        return false;
    }

    private char Peek()
    {
        if (_position >= _formula.Length)
            return '\0';
        return _formula[_position];
    }
}
