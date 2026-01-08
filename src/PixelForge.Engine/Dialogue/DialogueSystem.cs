using System.Text.Json;
using System.Text.Json.Serialization;

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
    private DialogueTree? _currentDialogue;
    private DialogueNode? _currentNode;

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
        // TODO: Implement condition checking with game state
        return true;
    }

    /// <summary>
    /// Execute dialogue actions.
    /// </summary>
    private void ExecuteActions(List<DialogueAction> actions)
    {
        // TODO: Implement action execution
    }
}
