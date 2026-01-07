# Vibe Coding Guide

Vibe Coding is PixelForge's AI-powered coding assistant that helps you create game features using natural language.

## Setup

### Get an API Key

1. Go to [Anthropic Console](https://console.anthropic.com/)
2. Create an account or sign in
3. Navigate to API Keys
4. Create a new API key
5. Copy your key

### Configure PixelForge

Set your API key as an environment variable:

**Windows:**
```powershell
setx ANTHROPIC_API_KEY "your-api-key-here"
```

**macOS/Linux:**
```bash
export ANTHROPIC_API_KEY="your-api-key-here"
```

Or add it to your editor settings in `EditorSettings.json`:
```json
{
  "anthropicApiKey": "your-api-key-here"
}
```

## Using Vibe Coding

### Opening the Vibe Panel

1. Click **Tools → Vibe Coding** or press **Ctrl+Shift+V**
2. The Vibe Coding panel appears on the right side

### Basic Usage

Simply describe what you want in natural language:

```
Create a quest system with multiple stages
```

The AI will generate complete C# code that integrates with your project!

### Example Prompts

#### Create a New System

```
Create a crafting system where players can combine items to create new items.
Include a crafting UI and recipe management.
```

#### Extend Existing Code

```
Add a day/night cycle to the game that changes lighting based on time.
The cycle should take 20 minutes for a full day.
```

#### Fix Issues

```
This code has a null reference exception when loading maps.
[Paste your code here]
```

#### Optimize Performance

```
Optimize this pathfinding algorithm:
[Paste your code here]
```

## Features

### Context-Aware Generation

Vibe Coding understands your project:
- Current map structure
- Existing scripts
- Database entries
- Engine API

### Code Explanation

Select any code and click **Explain** to get a detailed explanation.

### Code Optimization

Select code and click **Optimize** to improve performance and readability.

### Debugging Help

Paste error messages and code to get fixing suggestions.

### Conversation History

Continue previous conversations by enabling "Remember Context".

## Advanced Features

### Custom Templates

Create reusable templates for common patterns:

```csharp
// Template: Status Effect System
public class StatusEffect
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    // AI will expand this based on your needs
}
```

### Project-Specific Context

Vibe Coding analyzes your project to provide relevant suggestions:
- Follows your naming conventions
- Matches your architecture patterns
- Integrates with existing systems

## Best Practices

### Be Specific

❌ "Make the game better"
✅ "Add a minimap that shows the player's position and nearby events"

### Provide Context

Include relevant information:
```
Create a skill tree system for the mage class.
Skills should cost skill points.
The tree should have 3 tiers.
Each tier requires previous tier completion.
```

### Iterate

Start simple and refine:
1. "Create a simple inventory system"
2. Review the code
3. "Add item categories and sorting"
4. Review again
5. "Add item stacking for consumables"

### Review Generated Code

Always review AI-generated code:
- Verify it matches your requirements
- Check for edge cases
- Test thoroughly
- Integrate carefully

## Examples

### Example 1: Quest System

**Prompt:**
```
Create a quest system with the following features:
- Quest tracking (active, completed, failed)
- Multiple objectives per quest
- Quest rewards (items, gold, experience)
- Quest journal UI
- Save/load quest progress
```

**Result:** Complete quest system with data models, manager class, and UI integration.

### Example 2: Combat System

**Prompt:**
```
Extend the battle system with:
- Elemental affinities (fire, ice, lightning)
- Combo attacks (chaining skills)
- Critical hits based on luck stat
- Status effects in battle
```

**Result:** Enhanced battle formulas and status effect integration.

### Example 3: Save System

**Prompt:**
```
Create a save system that:
- Saves player position, inventory, and quest progress
- Supports multiple save slots
- Auto-save every 5 minutes
- Cloud save integration (optional)
```

**Result:** Complete save/load system with serialization.

## Troubleshooting

### API Key Not Working
- Verify key is set correctly
- Check for typos
- Ensure you have API credits

### Generation Too Slow
- Reduce context size
- Use more specific prompts
- Disable conversation history

### Code Doesn't Compile
- Review generated code
- Check namespace imports
- Verify API references
- Ask AI to fix compilation errors

### Not Understanding Context
- Provide more specific details
- Include code examples
- Reference specific files or systems

## Tips and Tricks

1. **Start with scaffolding**: Ask AI to create basic structure first
2. **Iterate incrementally**: Add features one at a time
3. **Use examples**: Provide example code for the style you want
4. **Be conversational**: Ask follow-up questions
5. **Export conversations**: Save useful exchanges for documentation

## Limitations

- AI may not understand very complex custom architectures
- Generated code should always be reviewed and tested
- Performance optimization may require manual tuning
- Very large codebases may need selective context

## Privacy and Security

- Code sent to Claude API for processing
- Don't include sensitive data (passwords, keys)
- Review generated code before committing
- API usage tracked for billing

## Cost Considerations

- Claude API charges per token
- Enable conversation history only when needed
- Use specific prompts to reduce token usage
- Monitor your API usage in Anthropic Console

---

For more help, see the [PixelForge Documentation](../README.md) or ask in [GitHub Discussions](https://github.com/yourusername/PixelForge/discussions).
