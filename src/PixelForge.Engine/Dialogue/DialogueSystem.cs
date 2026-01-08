using System.Text.Json;
using System.Text.Json.Serialization;
using PixelForge.Engine.Core;
using PixelForge.Engine.Quest;

namespace PixelForge.Engine.Dialogue;

/// <summary>
/// Dialogue tree for NPC conversations.
/// </summary>
public class DialogueTree
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Dialogue";

    [JsonPropertyName("startNode")]
    public string StartNodeId { get; set; } = string.Empty;

    [JsonPropertyName("nodes")]
    public List<DialogueNode> Nodes { get; set; } = new();
}

/// <summary>
/// Single dialogue node with text and choices.
/// </summary>
public class DialogueNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("nodeType")]
    public NodeType NodeType { get; set; }

    [JsonPropertyName("speakerName")]
    public string? SpeakerName { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("stringKey")]
    public string? StringKey { get; set; } // For localization

    [JsonPropertyName("choices")]
    public List<DialogueChoice> Choices { get; set; } = new();

    [JsonPropertyName("conditions")]
    public List<DialogueCondition> Conditions { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<DialogueAction> Actions { get; set; } = new();
}

/// <summary>
/// Choice option in dialogue.
/// </summary>
public class DialogueChoice
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("stringKey")]
    public string? StringKey { get; set; }

    [JsonPropertyName("nextNodeId")]
    public string? NextNodeId { get; set; }

    [JsonPropertyName("conditions")]
    public List<DialogueCondition> Conditions { get; set; } = new();
}

/// <summary>
/// Condition for showing dialogue/choice.
/// </summary>
public class DialogueCondition
{
    [JsonPropertyName("type")]
    public ConditionType Type { get; set; }

    [JsonPropertyName("parameter")]
    public string Parameter { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Action to perform when node is activated.
/// </summary>
public class DialogueAction
{
    [JsonPropertyName("type")]
    public ActionType Type { get; set; }

    [JsonPropertyName("parameter")]
    public string Parameter { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public enum NodeType
{
    Standard,
    Choice,
    End
}

public enum ConditionType
{
    Switch,
    Variable,
    HasItem,
    QuestComplete
}

public enum ActionType
{
    SetSwitch,
    SetVariable,
    GiveItem,
    StartQuest,
    CompleteQuest
}

/// <summary>
/// Manages dialogue trees and conversations.
/// </summary>
public class DialogueManager
{
    private readonly Dictionary<string, DialogueTree> _dialogues = new();
    private readonly Dictionary<string, string> _strings = new(); // Localization strings
    private readonly GameState _gameState;
    private readonly QuestManager _questManager;
    private DialogueTree? _currentDialogue;
    private DialogueNode? _currentNode;

    public DialogueManager(GameState gameState, QuestManager questManager)
    {
        _gameState = gameState;
        _questManager = questManager;
    }

    /// <summary>
    /// Load dialogue from file.
    /// </summary>
    public void LoadDialogue(string path)
    {
        string json = File.ReadAllText(path);
        var dialogue = JsonSerializer.Deserialize<DialogueTree>(json);
        if (dialogue != null)
        {
            _dialogues[dialogue.Id] = dialogue;
        }
    }

    /// <summary>
    /// Load string file for localization.
    /// </summary>
    public void LoadStrings(string path)
    {
        string json = File.ReadAllText(path);
        var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (strings != null)
        {
            foreach (var kvp in strings)
            {
                _strings[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Start a dialogue.
    /// </summary>
    public bool StartDialogue(string dialogueId)
    {
        if (!_dialogues.TryGetValue(dialogueId, out var dialogue))
            return false;

        _currentDialogue = dialogue;
        var startNode = dialogue.Nodes.FirstOrDefault(n => n.Id == dialogue.StartNodeId);

        if (startNode == null)
            return false;

        _currentNode = startNode;
        return true;
    }

    /// <summary>
    /// Get current dialogue text (with localization support).
    /// </summary>
    public string? GetCurrentText()
    {
        if (_currentNode == null)
            return null;

        // Check for localized string
        if (!string.IsNullOrEmpty(_currentNode.StringKey) && _strings.TryGetValue(_currentNode.StringKey, out string? localizedText))
        {
            return localizedText;
        }

        return _currentNode.Text;
    }

    /// <summary>
    /// Get current speaker name.
    /// </summary>
    public string? GetCurrentSpeaker()
    {
        return _currentNode?.SpeakerName;
    }

    /// <summary>
    /// Get available choices (filtered by conditions).
    /// </summary>
    public List<DialogueChoice> GetAvailableChoices()
    {
        if (_currentNode == null)
            return new List<DialogueChoice>();

        return _currentNode.Choices.Where(c => CheckConditions(c.Conditions)).ToList();
    }

    /// <summary>
    /// Select a choice and advance dialogue.
    /// </summary>
    public bool SelectChoice(int choiceIndex)
    {
        var choices = GetAvailableChoices();
        if (choiceIndex < 0 || choiceIndex >= choices.Count)
            return false;

        var choice = choices[choiceIndex];

        // Execute current node actions
        ExecuteActions(_currentNode?.Actions ?? new List<DialogueAction>());

        // Move to next node
        if (string.IsNullOrEmpty(choice.NextNodeId))
        {
            EndDialogue();
            return false;
        }

        var nextNode = _currentDialogue?.Nodes.FirstOrDefault(n => n.Id == choice.NextNodeId);
        if (nextNode == null)
        {
            EndDialogue();
            return false;
        }

        _currentNode = nextNode;
        return true;
    }

    /// <summary>
    /// End current dialogue.
    /// </summary>
    public void EndDialogue()
    {
        _currentDialogue = null;
        _currentNode = null;
    }

    /// <summary>
    /// Check if dialogue is active.
    /// </summary>
    public bool IsActive => _currentNode != null;

    /// <summary>
    /// Check dialogue conditions.
    /// </summary>
    private bool CheckConditions(List<DialogueCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            switch (condition.Type)
            {
                case ConditionType.Switch:
                    if (!TryParseInt(condition.Parameter, out int switchId)
                        || !TryParseBool(condition.Value, out bool switchValue))
                    {
                        LogWarning($"Invalid switch condition parameters: '{condition.Parameter}'='{condition.Value}'.");
                        return false;
                    }

                    if (_gameState.GetSwitch(switchId) != switchValue)
                        return false;
                    break;
                case ConditionType.Variable:
                    if (!TryParseInt(condition.Parameter, out int variableId)
                        || !TryParseInt(condition.Value, out int expectedValue))
                    {
                        LogWarning($"Invalid variable condition parameters: '{condition.Parameter}'='{condition.Value}'.");
                        return false;
                    }

                    if (_gameState.GetVariable(variableId) != expectedValue)
                        return false;
                    break;
                case ConditionType.HasItem:
                    if (string.IsNullOrWhiteSpace(condition.Parameter))
                    {
                        LogWarning("HasItem condition missing item id.");
                        return false;
                    }

                    int requiredCount = 1;
                    if (!string.IsNullOrWhiteSpace(condition.Value)
                        && !TryParseInt(condition.Value, out requiredCount))
                    {
                        LogWarning($"Invalid HasItem condition count: '{condition.Value}'.");
                        return false;
                    }

                    if (!_gameState.HasItem(condition.Parameter, requiredCount))
                        return false;
                    break;
                case ConditionType.QuestComplete:
                    if (string.IsNullOrWhiteSpace(condition.Parameter))
                    {
                        LogWarning("QuestComplete condition missing quest id.");
                        return false;
                    }

                    if (!_questManager.IsQuestCompleted(condition.Parameter))
                        return false;
                    break;
                default:
                    LogWarning($"Unknown condition type '{condition.Type}'.");
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Execute dialogue actions.
    /// </summary>
    private void ExecuteActions(List<DialogueAction> actions)
    {
        foreach (var action in actions)
        {
            switch (action.Type)
            {
                case ActionType.SetSwitch:
                    if (!TryParseInt(action.Parameter, out int switchId)
                        || !TryParseBool(action.Value, out bool switchValue))
                    {
                        LogWarning($"Invalid SetSwitch action parameters: '{action.Parameter}'='{action.Value}'.");
                        continue;
                    }

                    _gameState.SetSwitch(switchId, switchValue);
                    break;
                case ActionType.SetVariable:
                    if (!TryParseInt(action.Parameter, out int variableId)
                        || !TryParseInt(action.Value, out int variableValue))
                    {
                        LogWarning($"Invalid SetVariable action parameters: '{action.Parameter}'='{action.Value}'.");
                        continue;
                    }

                    _gameState.SetVariable(variableId, variableValue);
                    break;
                case ActionType.GiveItem:
                    if (string.IsNullOrWhiteSpace(action.Parameter))
                    {
                        LogWarning("GiveItem action missing item id.");
                        continue;
                    }

                    int itemCount = 1;
                    if (!string.IsNullOrWhiteSpace(action.Value)
                        && !TryParseInt(action.Value, out itemCount))
                    {
                        LogWarning($"Invalid GiveItem action count: '{action.Value}'.");
                        continue;
                    }

                    if (itemCount <= 0)
                    {
                        LogWarning($"GiveItem action count must be positive: '{itemCount}'.");
                        continue;
                    }

                    _gameState.AddItem(action.Parameter, itemCount);
                    break;
                case ActionType.StartQuest:
                    if (string.IsNullOrWhiteSpace(action.Parameter))
                    {
                        LogWarning("StartQuest action missing quest id.");
                        continue;
                    }

                    if (!_questManager.StartQuest(action.Parameter))
                        LogWarning($"Failed to start quest '{action.Parameter}'.");
                    break;
                case ActionType.CompleteQuest:
                    if (string.IsNullOrWhiteSpace(action.Parameter))
                    {
                        LogWarning("CompleteQuest action missing quest id.");
                        continue;
                    }

                    if (!_questManager.CompleteQuest(action.Parameter))
                        LogWarning($"Failed to complete quest '{action.Parameter}'.");
                    break;
                default:
                    LogWarning($"Unknown action type '{action.Type}'.");
                    break;
            }
        }
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value, out result);
    }

    private static bool TryParseBool(string value, out bool result)
    {
        return bool.TryParse(value, out result);
    }

    private static void LogWarning(string message)
    {
        Console.WriteLine($"[DialogueManager] {message}");
    }
}
