using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using PixelForge.Engine.Core;
using PixelForge.Engine.RPG;

namespace PixelForge.Engine.Quest;

/// <summary>
/// Quest definition.
/// </summary>
public class Quest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; set; } = "New Quest";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("objectives")]
    public ObservableCollection<QuestObjective> Objectives { get; set; } = new();

    [JsonPropertyName("rewards")]
    public QuestRewards Rewards { get; set; } = new();

    [JsonPropertyName("requirements")]
    public QuestRequirements? Requirements { get; set; }
}

/// <summary>
/// Quest objective.
/// </summary>
public class QuestObjective
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public ObjectiveType Type { get; set; }

    [JsonPropertyName("targetId")]
    public string? TargetId { get; set; }

    [JsonPropertyName("targetCount")]
    public int TargetCount { get; set; } = 1;

    [JsonPropertyName("currentCount")]
    public int CurrentCount { get; set; }

    [JsonPropertyName("optional")]
    public bool Optional { get; set; }

    public bool IsComplete => CurrentCount >= TargetCount;
}

/// <summary>
/// Quest rewards.
/// </summary>
public class QuestRewards
{
    [JsonPropertyName("experience")]
    public int Experience { get; set; }

    [JsonPropertyName("gold")]
    public int Gold { get; set; }

    [JsonPropertyName("items")]
    public Dictionary<string, int> Items { get; set; } = new();
}

/// <summary>
/// Quest requirements.
/// </summary>
public class QuestRequirements
{
    [JsonPropertyName("level")]
    public int? MinLevel { get; set; }

    [JsonPropertyName("previousQuests")]
    public ObservableCollection<string> PreviousQuests { get; set; } = new();

    [JsonPropertyName("flags")]
    public ObservableCollection<QuestFlagRequirement> Flags { get; set; } = new();
}

/// <summary>
/// Quest flag requirement.
/// </summary>
public class QuestFlagRequirement
{
    [JsonPropertyName("switchId")]
    public int SwitchId { get; set; }

    [JsonPropertyName("value")]
    public bool Value { get; set; } = true;
}

/// <summary>
/// Runtime quest instance.
/// </summary>
public class ActiveQuest
{
    public Quest QuestData { get; set; } = null!;
    public QuestStatus Status { get; set; } = QuestStatus.Active;
    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? CompletionTime { get; set; }

    public bool IsComplete => QuestData.Objectives.All(o => o.Optional || o.IsComplete);
}

public enum ObjectiveType
{
    Kill,           // Kill X enemies
    Collect,        // Collect X items
    Talk,           // Talk to NPC
    Reach,          // Reach location
    Custom          // Custom via events
}

public enum QuestStatus
{
    Active,
    Completed,
    Failed
}

/// <summary>
/// Manages quest progression.
/// </summary>
public class QuestManager
{
    private readonly Dictionary<string, Quest> _availableQuests = new();
    private readonly Dictionary<string, ActiveQuest> _activeQuests = new();
    private readonly HashSet<string> _completedQuests = new();
    private readonly GameState? _gameState;
    private readonly PartyManager? _partyManager;

    public QuestManager(GameState? gameState = null, PartyManager? partyManager = null)
    {
        _gameState = gameState;
        _partyManager = partyManager;
    }

    /// <summary>
    /// Register a quest as available.
    /// </summary>
    public void RegisterQuest(Quest quest)
    {
        _availableQuests[quest.Id] = quest;
    }

    /// <summary>
    /// Start a quest.
    /// </summary>
    public bool StartQuest(string questId)
    {
        if (!_availableQuests.TryGetValue(questId, out var quest))
            return false;

        if (_activeQuests.ContainsKey(questId))
            return false;

        if (_completedQuests.Contains(questId))
            return false;

        // Check requirements
        if (!MeetsRequirements(quest))
            return false;

        // Create active quest
        var activeQuest = new ActiveQuest
        {
            QuestData = quest,
            Status = QuestStatus.Active,
            StartTime = DateTime.Now
        };

        _activeQuests[questId] = activeQuest;
        return true;
    }

    /// <summary>
    /// Update quest objective progress.
    /// </summary>
    public void UpdateObjective(string questId, string objectiveId, int increment = 1)
    {
        if (!_activeQuests.TryGetValue(questId, out var activeQuest))
            return;

        var objective = activeQuest.QuestData.Objectives.FirstOrDefault(o => o.Id == objectiveId);
        if (objective == null)
            return;

        objective.CurrentCount = Math.Min(objective.CurrentCount + increment, objective.TargetCount);

        // Check if quest is complete
        if (activeQuest.IsComplete)
        {
            CompleteQuest(questId);
        }
    }

    /// <summary>
    /// Complete a quest.
    /// </summary>
    public bool CompleteQuest(string questId)
    {
        if (!_activeQuests.TryGetValue(questId, out var activeQuest))
            return false;

        if (!activeQuest.IsComplete)
            return false;

        activeQuest.Status = QuestStatus.Completed;
        activeQuest.CompletionTime = DateTime.Now;

        _activeQuests.Remove(questId);
        _completedQuests.Add(questId);

        ApplyRewards(activeQuest.QuestData);

        return true;
    }

    /// <summary>
    /// Fail a quest.
    /// </summary>
    public void FailQuest(string questId)
    {
        if (_activeQuests.TryGetValue(questId, out var activeQuest))
        {
            activeQuest.Status = QuestStatus.Failed;
            _activeQuests.Remove(questId);
        }
    }

    /// <summary>
    /// Get all active quests.
    /// </summary>
    public List<ActiveQuest> GetActiveQuests()
    {
        return _activeQuests.Values.ToList();
    }

    /// <summary>
    /// Check if quest is completed.
    /// </summary>
    public bool IsQuestCompleted(string questId)
    {
        return _completedQuests.Contains(questId);
    }

    /// <summary>
    /// Get quest by ID.
    /// </summary>
    public ActiveQuest? GetActiveQuest(string questId)
    {
        return _activeQuests.TryGetValue(questId, out var quest) ? quest : null;
    }

    private bool MeetsRequirements(Quest quest)
    {
        if (quest.Requirements == null)
            return true;

        if (quest.Requirements.PreviousQuests.Any(pq => !_completedQuests.Contains(pq)))
            return false;

        if (quest.Requirements.MinLevel != null)
        {
            if (_partyManager == null)
                return false;

            var highestLevel = _partyManager.Party.Count > 0
                ? _partyManager.Party.Max(actor => actor.Level)
                : 0;

            if (highestLevel < quest.Requirements.MinLevel.Value)
                return false;
        }

        if (quest.Requirements.Flags.Any())
        {
            if (_gameState == null)
                return false;

            foreach (var flag in quest.Requirements.Flags)
            {
                if (_gameState.GetSwitch(flag.SwitchId) != flag.Value)
                    return false;
            }
        }

        return true;
    }

    private void ApplyRewards(Quest quest)
    {
        if (_gameState != null)
        {
            if (quest.Rewards.Gold != 0)
                _gameState.PartyGold += quest.Rewards.Gold;

            foreach (var rewardItem in quest.Rewards.Items)
            {
                if (rewardItem.Value > 0)
                    _gameState.AddItem(rewardItem.Key, rewardItem.Value);
            }
        }

        if (_partyManager != null && quest.Rewards.Experience > 0)
        {
            _partyManager.GainExperience(quest.Rewards.Experience);
        }
    }
}
