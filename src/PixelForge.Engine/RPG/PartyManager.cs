using PixelForge.Shared.Models.Database;

namespace PixelForge.Engine.RPG;

/// <summary>
/// Manages the party of actors.
/// </summary>
public class PartyManager
{
    private readonly List<GameActor> _party = new();
    private readonly int _maxPartySize = 4;

    public IReadOnlyList<GameActor> Party => _party.AsReadOnly();
    public int Gold { get; set; }

    /// <summary>
    /// Add actor to party.
    /// </summary>
    public bool AddActor(GameActor actor)
    {
        if (_party.Count >= _maxPartySize)
            return false;

        if (_party.Any(a => a.ActorId == actor.ActorId))
            return false;

        _party.Add(actor);
        return true;
    }

    /// <summary>
    /// Remove actor from party.
    /// </summary>
    public bool RemoveActor(string actorId)
    {
        var actor = _party.FirstOrDefault(a => a.ActorId == actorId);
        if (actor == null)
            return false;

        _party.Remove(actor);
        return true;
    }

    /// <summary>
    /// Clear all actors from the party.
    /// </summary>
    public void Clear()
    {
        _party.Clear();
    }

    /// <summary>
    /// Get actor by ID.
    /// </summary>
    public GameActor? GetActor(string actorId)
    {
        return _party.FirstOrDefault(a => a.ActorId == actorId);
    }

    /// <summary>
    /// Get alive actors.
    /// </summary>
    public IEnumerable<GameActor> GetAliveActors()
    {
        return _party.Where(a => a.IsAlive);
    }

    /// <summary>
    /// Check if party is defeated.
    /// </summary>
    public bool IsDefeated => !_party.Any(a => a.IsAlive);

    /// <summary>
    /// Heal all actors.
    /// </summary>
    public void HealAll()
    {
        foreach (var actor in _party)
        {
            var stats = actor.GetCurrentStats();
            actor.CurrentHp = stats.MaxHp;
            actor.CurrentMp = stats.MaxMp;
            actor.CurrentTp = 0;
            actor.States.Clear();
        }
    }

    /// <summary>
    /// Gain experience for all actors.
    /// </summary>
    public List<GameActor> GainExperience(int exp)
    {
        var leveledUpActors = new List<GameActor>();

        foreach (var actor in GetAliveActors())
        {
            if (actor.GainExperience(exp))
            {
                leveledUpActors.Add(actor);
            }
        }

        return leveledUpActors;
    }

    /// <summary>
    /// Gain gold.
    /// </summary>
    public void GainGold(int amount)
    {
        Gold += amount;
    }

    /// <summary>
    /// Spend gold.
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (Gold < amount)
            return false;

        Gold -= amount;
        return true;
    }
}
