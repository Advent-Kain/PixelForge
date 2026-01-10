using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Engine.Dialogue;
using PixelForge.Editor.Views;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for dialogue tree editor.
/// </summary>
public partial class DialogueEditorViewModel : ObservableObject
{
    private readonly string _dialoguePath = "Content/Dialogues";

    [ObservableProperty]
    private ObservableCollection<DialogueTreeInfo> _dialogueTrees = new();

    [ObservableProperty]
    private DialogueTreeInfo? _selectedTree;

    [ObservableProperty]
    private DialogueTree? _currentTree;

    [ObservableProperty]
    private DialogueNodeViewModel? _selectedNode;

    [ObservableProperty]
    private ObservableCollection<DialogueNodeViewModel> _nodes = new();

    [ObservableProperty]
    private string _newTreeName = string.Empty;

    public IReadOnlyList<ConditionType> ConditionTypes { get; } = Enum.GetValues<ConditionType>();

    public ICommand NewTreeCommand { get; }
    public ICommand LoadTreeCommand { get; }
    public ICommand SaveTreeCommand { get; }
    public ICommand DeleteTreeCommand { get; }
    public ICommand AddNodeCommand { get; }
    public ICommand DeleteNodeCommand { get; }
    public ICommand AddChoiceCommand { get; }
    public ICommand OpenStringEditorCommand { get; }

    public DialogueEditorViewModel()
    {
        NewTreeCommand = new RelayCommand(CreateNewTree);
        LoadTreeCommand = new RelayCommand(LoadTree, CanLoadTree);
        SaveTreeCommand = new RelayCommand(SaveTree, CanSaveTree);
        DeleteTreeCommand = new RelayCommand(DeleteTree, CanDeleteTree);
        AddNodeCommand = new RelayCommand(AddNode);
        DeleteNodeCommand = new RelayCommand(DeleteNode, CanDeleteNode);
        AddChoiceCommand = new RelayCommand(AddChoice, CanAddChoice);
        OpenStringEditorCommand = new RelayCommand(OpenStringEditor);

        LoadDialogueTrees();
    }

    private void LoadDialogueTrees()
    {
        DialogueTrees.Clear();

        if (!Directory.Exists(_dialoguePath))
            Directory.CreateDirectory(_dialoguePath);

        var files = Directory.GetFiles(_dialoguePath, "*.json");
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            DialogueTrees.Add(new DialogueTreeInfo
            {
                Name = name,
                FilePath = file
            });
        }
    }

    private void CreateNewTree()
    {
        if (string.IsNullOrWhiteSpace(NewTreeName))
            return;

        var tree = new DialogueTree
        {
            Id = Guid.NewGuid().ToString(),
            Name = NewTreeName
        };

        // Add initial root node
        var rootNode = new DialogueNode
        {
            Id = Guid.NewGuid().ToString(),
            StringKey = $"dialogue.{NewTreeName}.root",
            Text = "Start conversation here...",
            NodeType = NodeType.Standard
        };

        tree.Nodes.Add(rootNode);
        tree.StartNodeId = rootNode.Id;

        CurrentTree = tree;
        LoadNodesFromTree();

        NewTreeName = string.Empty;
        SaveTree();
        LoadDialogueTrees();
    }

    private bool CanLoadTree() => SelectedTree != null;

    private void LoadTree()
    {
        if (SelectedTree == null)
            return;

        try
        {
            var json = File.ReadAllText(SelectedTree.FilePath);
            CurrentTree = JsonSerializer.Deserialize<DialogueTree>(json);

            if (CurrentTree != null)
            {
                LoadNodesFromTree();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading dialogue tree: {ex.Message}");
        }
    }

    private bool CanSaveTree() => CurrentTree != null;

    private void SaveTree()
    {
        if (CurrentTree == null)
            return;

        try
        {
            if (!Directory.Exists(_dialoguePath))
                Directory.CreateDirectory(_dialoguePath);

            // Save nodes back to tree
            CurrentTree.Nodes.Clear();
            foreach (var nodeVm in Nodes)
            {
                CurrentTree.Nodes.Add(nodeVm.ToDialogueNode());
            }

            var filePath = Path.Combine(_dialoguePath, $"{CurrentTree.Name}.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(CurrentTree, options);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Saved dialogue tree: {CurrentTree.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving dialogue tree: {ex.Message}");
        }
    }

    private bool CanDeleteTree() => SelectedTree != null;

    private void DeleteTree()
    {
        if (SelectedTree == null)
            return;

        try
        {
            File.Delete(SelectedTree.FilePath);
            LoadDialogueTrees();

            if (CurrentTree?.Name == SelectedTree.Name)
            {
                CurrentTree = null;
                Nodes.Clear();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting dialogue tree: {ex.Message}");
        }
    }

    private void AddNode()
    {
        if (CurrentTree == null)
            return;

        var newNode = new DialogueNodeViewModel
        {
            Id = Guid.NewGuid().ToString(),
            StringKey = $"dialogue.{CurrentTree.Name}.node_{Nodes.Count + 1}",
            Text = "New dialogue node...",
            NodeType = NodeType.Standard,
            SpeakerName = "NPC"
        };

        Nodes.Add(newNode);
        SelectedNode = newNode;
    }

    private bool CanDeleteNode() => SelectedNode != null;

    private void DeleteNode()
    {
        if (SelectedNode == null)
            return;

        // Remove references to this node
        foreach (var node in Nodes)
        {
            var choicesToRemove = node.Choices
                .Where(choice => choice.NextNodeId == SelectedNode.Id)
                .ToList();

            foreach (var choice in choicesToRemove)
            {
                node.Choices.Remove(choice);
            }

            if (node.NextNodeId == SelectedNode.Id)
                node.NextNodeId = null;
        }

        Nodes.Remove(SelectedNode);
        SelectedNode = null;
    }

    private bool CanAddChoice() => SelectedNode != null;

    private void AddChoice()
    {
        if (SelectedNode == null)
            return;

        var newChoice = new DialogueChoiceViewModel
        {
            Id = Guid.NewGuid().ToString(),
            StringKey = $"{SelectedNode.StringKey}.choice_{SelectedNode.Choices.Count + 1}",
            Text = "Choice text...",
            NextNodeId = null
        };

        SelectedNode.Choices.Add(newChoice);
    }

    private void OpenStringEditor()
    {
        // This would open a separate string editor window
        var stringEditor = new StringEditorWindow();
        stringEditor.Show();
    }

    private void LoadNodesFromTree()
    {
        Nodes.Clear();

        if (CurrentTree == null)
            return;

        foreach (var node in CurrentTree.Nodes)
        {
            var nodeVm = DialogueNodeViewModel.FromDialogueNode(node);
            Nodes.Add(nodeVm);
        }

        if (Nodes.Count > 0)
            SelectedNode = Nodes[0];
    }
}

/// <summary>
/// Dialogue tree info for tree list.
/// </summary>
public class DialogueTreeInfo
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// View model for a dialogue node.
/// </summary>
public partial class DialogueNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _stringKey = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string? _speakerName;

    [ObservableProperty]
    private NodeType _nodeType;

    [ObservableProperty]
    private string? _nextNodeId;

    [ObservableProperty]
    private ObservableCollection<DialogueChoiceViewModel> _choices = new();

    [ObservableProperty]
    private List<DialogueCondition> _conditions = new();

    [ObservableProperty]
    private List<DialogueAction> _actions = new();

    public static DialogueNodeViewModel FromDialogueNode(DialogueNode node)
    {
        var vm = new DialogueNodeViewModel
        {
            Id = node.Id,
            StringKey = node.StringKey ?? string.Empty,
            Text = node.Text ?? string.Empty,
            SpeakerName = node.SpeakerName,
            NodeType = node.NodeType,
            NextNodeId = node.NextNodeId,
            Conditions = node.Conditions?.ToList() ?? new(),
            Actions = node.Actions?.ToList() ?? new()
        };

        if (node.Choices != null)
        {
            foreach (var choice in node.Choices)
            {
                vm.Choices.Add(DialogueChoiceViewModel.FromDialogueChoice(choice));
            }
        }

        return vm;
    }

    public DialogueNode ToDialogueNode()
    {
        return new DialogueNode
        {
            Id = Id,
            StringKey = StringKey,
            Text = Text,
            SpeakerName = SpeakerName,
            NodeType = NodeType,
            NextNodeId = NextNodeId,
            Choices = Choices.Select(c => c.ToDialogueChoice()).ToList(),
            Conditions = Conditions,
            Actions = Actions
        };
    }

    [RelayCommand]
    private void AddCondition()
    {
        var updated = Conditions?.ToList() ?? new List<DialogueCondition>();
        updated.Add(new DialogueCondition { Type = ConditionType.Switch });
        Conditions = updated;
    }

    [RelayCommand]
    private void RemoveCondition(DialogueCondition condition)
    {
        if (condition == null)
            return;

        var updated = Conditions?.ToList() ?? new List<DialogueCondition>();
        updated.Remove(condition);
        Conditions = updated;
    }
}

/// <summary>
/// View model for a dialogue choice.
/// </summary>
public partial class DialogueChoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _stringKey = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string? _nextNodeId;

    [ObservableProperty]
    private List<DialogueCondition> _conditions = new();

    public static DialogueChoiceViewModel FromDialogueChoice(DialogueChoice choice)
    {
        return new DialogueChoiceViewModel
        {
            Id = choice.Id,
            StringKey = choice.StringKey ?? string.Empty,
            Text = choice.Text ?? string.Empty,
            NextNodeId = choice.NextNodeId,
            Conditions = choice.Conditions?.ToList() ?? new()
        };
    }

    public DialogueChoice ToDialogueChoice()
    {
        return new DialogueChoice
        {
            Id = Id,
            StringKey = StringKey,
            Text = Text,
            NextNodeId = NextNodeId,
            Conditions = Conditions
        };
    }
}
