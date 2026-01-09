using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PixelForge.VibeCode.ClaudeClient;
using PixelForge.VibeCode.ContextBuilding;

namespace PixelForge.VibeCode.CodeGeneration;

/// <summary>
/// Generates C# code from natural language descriptions.
/// </summary>
public class CodeGenerator
{
    private readonly ClaudeApiClient _claudeClient;
    private readonly ProjectContextBuilder _contextBuilder;
    private readonly List<Message> _conversationHistory = new();

    public CodeGenerator(string apiKey, string projectPath)
    {
        _claudeClient = new ClaudeApiClient(apiKey);
        _contextBuilder = new ProjectContextBuilder(projectPath);
    }

    /// <summary>
    /// Generate code from a natural language description.
    /// </summary>
    public async Task<CodeGenerationResult> GenerateAsync(string prompt, CodeGenerationOptions? options = null)
    {
        options ??= new CodeGenerationOptions();

        try
        {
            // Build context
            string context = await _contextBuilder.BuildContextAsync(options.IncludeProjectStructure);

            // Create system prompt
            string systemPrompt = BuildSystemPrompt(context, options);

            // Send request
            var response = await _claudeClient.SendMessageAsync(
                prompt,
                systemPrompt,
                options.IncludeConversationHistory ? _conversationHistory : null,
                options.MaxTokens
            );

            if (response == null || !response.Content.Any())
            {
                return new CodeGenerationResult
                {
                    Success = false,
                    ErrorMessage = "No response from Claude API"
                };
            }

            // Extract code from response
            string generatedCode = response.Content.First().Text;

            // Update conversation history
            if (options.IncludeConversationHistory)
            {
                _conversationHistory.Add(new Message
                {
                    Role = "user",
                    Content = prompt
                });
                _conversationHistory.Add(new Message
                {
                    Role = "assistant",
                    Content = generatedCode
                });
            }

            return new CodeGenerationResult
            {
                Success = true,
                GeneratedCode = generatedCode,
                TokensUsed = (response.Usage?.InputTokens ?? 0) + (response.Usage?.OutputTokens ?? 0)
            };
        }
        catch (Exception ex)
        {
            return new CodeGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating code: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Explain existing code.
    /// </summary>
    public async Task<string?> ExplainCodeAsync(string code)
    {
        string prompt = $@"Explain the following C# code in detail:

```csharp
{code}
```

Include:
1. What the code does
2. Key design patterns or techniques used
3. Any potential improvements or issues";

        var result = await GenerateAsync(prompt, new CodeGenerationOptions
        {
            IncludeProjectStructure = false,
            MaxTokens = 2048
        });

        return result.GeneratedCode;
    }

    /// <summary>
    /// Optimize existing code.
    /// </summary>
    public async Task<string?> OptimizeCodeAsync(string code)
    {
        string prompt = $@"Optimize the following C# code for performance and readability:

```csharp
{code}
```

Return only the optimized code without explanations.";

        var result = await GenerateAsync(prompt, new CodeGenerationOptions
        {
            IncludeProjectStructure = false
        });

        return result.GeneratedCode;
    }

    /// <summary>
    /// Fix errors in code.
    /// </summary>
    public async Task<string?> FixCodeAsync(string code, string errorMessage)
    {
        string prompt = $@"Fix the following C# code that has this error:

Error: {errorMessage}

Code:
```csharp
{code}
```

Return only the fixed code without explanations.";

        var result = await GenerateAsync(prompt, new CodeGenerationOptions
        {
            IncludeProjectStructure = false
        });

        return result.GeneratedCode;
    }

    /// <summary>
    /// Clear conversation history.
    /// </summary>
    public void ClearHistory()
    {
        _conversationHistory.Clear();
    }

    private string BuildSystemPrompt(string context, CodeGenerationOptions options)
    {
        return $@"You are an expert C# programmer helping to build PixelForge, a 2D game engine similar to RPG Maker MZ.

{context}

Guidelines:
- Generate clean, well-documented C# code
- Follow C# naming conventions and best practices
- Use modern C# features (records, pattern matching, etc.)
- Include XML documentation comments for public members
- Implement proper error handling
- Consider performance implications
- Make code testable and maintainable

{(options.CodeOnly ? "Return ONLY the code without any explanations or markdown formatting." : "")}";
    }
}

/// <summary>
/// Options for code generation.
/// </summary>
public class CodeGenerationOptions
{
    public bool IncludeProjectStructure { get; set; } = true;
    public bool IncludeConversationHistory { get; set; } = true;
    public bool CodeOnly { get; set; } = false;
    public int MaxTokens { get; set; } = 4096;
}

/// <summary>
/// Result of code generation.
/// </summary>
public class CodeGenerationResult
{
    public bool Success { get; set; }
    public string? GeneratedCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int TokensUsed { get; set; }
}
