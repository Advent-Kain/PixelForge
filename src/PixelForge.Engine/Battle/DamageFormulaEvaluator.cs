using System.Text.RegularExpressions;
using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.Battle;

/// <summary>
/// Evaluates RPG-style damage formulas like "a.atk * 4 - b.def * 2".
/// Supports standard RPG Maker MZ formula syntax.
/// </summary>
public static class DamageFormulaEvaluator
{
    private static readonly Random _random = new();

    /// <summary>
    /// Evaluate a damage formula string.
    /// </summary>
    /// <param name="formula">Formula string (e.g., "a.atk * 4 - b.def * 2")</param>
    /// <param name="attackerStats">Attacker's stats (referenced as 'a')</param>
    /// <param name="defenderStats">Defender's stats (referenced as 'b')</param>
    /// <param name="skillLevel">Optional skill level for scaling (referenced as 'v')</param>
    /// <returns>Calculated damage value</returns>
    public static int Evaluate(string formula, ActorStats attackerStats, ActorStats defenderStats, int skillLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return 0;

        try
        {
            // Replace stat references with actual values
            string expression = SubstituteStatReferences(formula, attackerStats, defenderStats, skillLevel);

            // Evaluate the mathematical expression
            double result = EvaluateExpression(expression);

            return (int)Math.Max(0, result);
        }
        catch
        {
            // Fallback to a basic calculation if formula parsing fails
            return Math.Max(1, attackerStats.Attack - defenderStats.Defense / 2);
        }
    }

    /// <summary>
    /// Apply variance to a damage value.
    /// </summary>
    /// <param name="baseDamage">Base damage amount</param>
    /// <param name="variance">Variance percentage (0-100)</param>
    /// <returns>Damage with variance applied</returns>
    public static int ApplyVariance(int baseDamage, int variance)
    {
        if (variance <= 0)
            return baseDamage;

        float varianceMultiplier = variance / 100f;
        float minMultiplier = 1f - varianceMultiplier;
        float maxMultiplier = 1f + varianceMultiplier;
        float randomMultiplier = minMultiplier + (float)_random.NextDouble() * (maxMultiplier - minMultiplier);

        return (int)(baseDamage * randomMultiplier);
    }

    /// <summary>
    /// Check if an attack is a critical hit.
    /// </summary>
    /// <param name="criticalRate">Critical rate percentage (0-100)</param>
    /// <param name="attackerLuck">Attacker's luck stat</param>
    /// <param name="defenderLuck">Defender's luck stat</param>
    /// <returns>True if critical hit</returns>
    public static bool IsCriticalHit(float criticalRate, int attackerLuck, int defenderLuck)
    {
        // Luck difference affects critical rate
        float luckModifier = (attackerLuck - defenderLuck) * 0.1f;
        float finalRate = Math.Clamp(criticalRate + luckModifier, 0f, 100f);

        return _random.NextDouble() * 100 < finalRate;
    }

    /// <summary>
    /// Apply critical multiplier to damage.
    /// </summary>
    /// <param name="damage">Base damage</param>
    /// <param name="criticalMultiplier">Critical damage multiplier (default 3x)</param>
    /// <returns>Critical damage</returns>
    public static int ApplyCritical(int damage, float criticalMultiplier = 3f)
    {
        return (int)(damage * criticalMultiplier);
    }

    /// <summary>
    /// Check if an attack hits.
    /// </summary>
    /// <param name="hitRate">Base hit rate percentage (0-100)</param>
    /// <param name="attackerAgility">Attacker's agility</param>
    /// <param name="defenderAgility">Defender's agility (for evasion)</param>
    /// <returns>True if attack hits</returns>
    public static bool CheckHit(float hitRate, int attackerAgility, int defenderAgility)
    {
        // Agility difference affects hit rate
        float agilityModifier = (attackerAgility - defenderAgility) * 0.5f;
        float finalRate = Math.Clamp(hitRate + agilityModifier, 5f, 100f); // Minimum 5% hit rate

        return _random.NextDouble() * 100 < finalRate;
    }

    /// <summary>
    /// Calculate full damage with all modifiers.
    /// </summary>
    public static DamageResult CalculateFullDamage(
        DamageFormula damageFormula,
        ActorStats attackerStats,
        ActorStats defenderStats,
        float hitRate = 100f,
        float criticalRate = 0f)
    {
        var result = new DamageResult();

        // Check hit
        if (!CheckHit(hitRate, attackerStats.Agility, defenderStats.Agility))
        {
            result.IsMiss = true;
            return result;
        }

        // Calculate base damage
        int baseDamage = Evaluate(damageFormula.Formula, attackerStats, defenderStats);

        // Check critical
        if (damageFormula.Critical)
        {
            result.IsCritical = IsCriticalHit(criticalRate, attackerStats.Luck, defenderStats.Luck);
            if (result.IsCritical)
            {
                baseDamage = ApplyCritical(baseDamage);
            }
        }

        // Apply variance
        result.Damage = ApplyVariance(baseDamage, damageFormula.Variance);

        return result;
    }

    /// <summary>
    /// Substitute stat references (a.atk, b.def, etc.) with actual values.
    /// </summary>
    private static string SubstituteStatReferences(string formula, ActorStats a, ActorStats b, int v)
    {
        // Create a copy for substitution
        string result = formula;

        // Attacker stats (a.*)
        result = Regex.Replace(result, @"\ba\.atk\b", a.Attack.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\ba\.def\b", a.Defense.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\ba\.mat\b", a.MagicAttack.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\ba\.mdf\b", a.MagicDefense.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\ba\.agi\b", a.Agility.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\ba\.luk\b", a.Luck.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\ba\.mhp\b", a.MaxHp.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\ba\.mmp\b", a.MaxMp.ToString(), RegexOptions.IgnoreCase);

        // Defender stats (b.*)
        result = Regex.Replace(result, @"\bb\.atk\b", b.Attack.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bb\.def\b", b.Defense.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bb\.mat\b", b.MagicAttack.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bb\.mdf\b", b.MagicDefense.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bb\.agi\b", b.Agility.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bb\.luk\b", b.Luck.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bb\.mhp\b", b.MaxHp.ToString(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bb\.mmp\b", b.MaxMp.ToString(), RegexOptions.IgnoreCase);

        // Skill level variable (v)
        result = Regex.Replace(result, @"\bv\b", v.ToString(), RegexOptions.IgnoreCase);

        return result;
    }

    /// <summary>
    /// Evaluate a mathematical expression string.
    /// Supports: +, -, *, /, parentheses, and Math functions.
    /// </summary>
    private static double EvaluateExpression(string expression)
    {
        // Remove whitespace
        expression = expression.Replace(" ", "");

        // Handle Math functions
        expression = ProcessMathFunctions(expression);

        // Parse and evaluate
        int pos = 0;
        return ParseExpression(expression, ref pos);
    }

    /// <summary>
    /// Process Math functions like Math.max, Math.min, Math.floor, etc.
    /// </summary>
    private static string ProcessMathFunctions(string expression)
    {
        // Math.max(a, b)
        expression = Regex.Replace(expression, @"Math\.max\(([^,]+),([^)]+)\)", m =>
        {
            double a = EvaluateExpression(m.Groups[1].Value);
            double b = EvaluateExpression(m.Groups[2].Value);
            return Math.Max(a, b).ToString();
        }, RegexOptions.IgnoreCase);

        // Math.min(a, b)
        expression = Regex.Replace(expression, @"Math\.min\(([^,]+),([^)]+)\)", m =>
        {
            double a = EvaluateExpression(m.Groups[1].Value);
            double b = EvaluateExpression(m.Groups[2].Value);
            return Math.Min(a, b).ToString();
        }, RegexOptions.IgnoreCase);

        // Math.floor(a)
        expression = Regex.Replace(expression, @"Math\.floor\(([^)]+)\)", m =>
        {
            double a = EvaluateExpression(m.Groups[1].Value);
            return Math.Floor(a).ToString();
        }, RegexOptions.IgnoreCase);

        // Math.ceil(a)
        expression = Regex.Replace(expression, @"Math\.ceil\(([^)]+)\)", m =>
        {
            double a = EvaluateExpression(m.Groups[1].Value);
            return Math.Ceiling(a).ToString();
        }, RegexOptions.IgnoreCase);

        // Math.abs(a)
        expression = Regex.Replace(expression, @"Math\.abs\(([^)]+)\)", m =>
        {
            double a = EvaluateExpression(m.Groups[1].Value);
            return Math.Abs(a).ToString();
        }, RegexOptions.IgnoreCase);

        // Math.pow(a, b)
        expression = Regex.Replace(expression, @"Math\.pow\(([^,]+),([^)]+)\)", m =>
        {
            double a = EvaluateExpression(m.Groups[1].Value);
            double b = EvaluateExpression(m.Groups[2].Value);
            return Math.Pow(a, b).ToString();
        }, RegexOptions.IgnoreCase);

        // Math.sqrt(a)
        expression = Regex.Replace(expression, @"Math\.sqrt\(([^)]+)\)", m =>
        {
            double a = EvaluateExpression(m.Groups[1].Value);
            return Math.Sqrt(a).ToString();
        }, RegexOptions.IgnoreCase);

        return expression;
    }

    /// <summary>
    /// Parse and evaluate expression with operator precedence.
    /// </summary>
    private static double ParseExpression(string expression, ref int pos)
    {
        double result = ParseTerm(expression, ref pos);

        while (pos < expression.Length)
        {
            char op = expression[pos];
            if (op != '+' && op != '-')
                break;

            pos++;
            double term = ParseTerm(expression, ref pos);

            if (op == '+')
                result += term;
            else
                result -= term;
        }

        return result;
    }

    private static double ParseTerm(string expression, ref int pos)
    {
        double result = ParseFactor(expression, ref pos);

        while (pos < expression.Length)
        {
            char op = expression[pos];
            if (op != '*' && op != '/')
                break;

            pos++;
            double factor = ParseFactor(expression, ref pos);

            if (op == '*')
                result *= factor;
            else if (factor != 0)
                result /= factor;
        }

        return result;
    }

    private static double ParseFactor(string expression, ref int pos)
    {
        // Handle negative numbers
        bool negative = false;
        if (pos < expression.Length && expression[pos] == '-')
        {
            negative = true;
            pos++;
        }

        double result;

        if (pos < expression.Length && expression[pos] == '(')
        {
            pos++; // Skip '('
            result = ParseExpression(expression, ref pos);
            if (pos < expression.Length && expression[pos] == ')')
                pos++; // Skip ')'
        }
        else
        {
            result = ParseNumber(expression, ref pos);
        }

        return negative ? -result : result;
    }

    private static double ParseNumber(string expression, ref int pos)
    {
        int start = pos;
        while (pos < expression.Length && (char.IsDigit(expression[pos]) || expression[pos] == '.'))
        {
            pos++;
        }

        string numberStr = expression.Substring(start, pos - start);
        return double.TryParse(numberStr, out double result) ? result : 0;
    }
}

/// <summary>
/// Result of a damage calculation.
/// </summary>
public class DamageResult
{
    public int Damage { get; set; }
    public bool IsCritical { get; set; }
    public bool IsMiss { get; set; }
    public string? Element { get; set; }
}
