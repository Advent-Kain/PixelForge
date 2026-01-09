using System.Text.Json;
using System.Linq;
using PixelForge.Shared.Models;

namespace PixelForge.Engine.Events;

internal static class EventCommandParameterReader
{
    public static int GetInt(object? value, int fallback = 0)
    {
        return value switch
        {
            null => fallback,
            int intValue => intValue,
            long longValue => (int)longValue,
            float floatValue => (int)floatValue,
            double doubleValue => (int)doubleValue,
            string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
            JsonElement jsonElement => GetInt(jsonElement, fallback),
            _ => fallback
        };
    }

    public static float GetFloat(object? value, float fallback = 0f)
    {
        return value switch
        {
            null => fallback,
            float floatValue => floatValue,
            double doubleValue => (float)doubleValue,
            int intValue => intValue,
            long longValue => longValue,
            string stringValue when float.TryParse(stringValue, out var parsed) => parsed,
            JsonElement jsonElement => GetFloat(jsonElement, fallback),
            _ => fallback
        };
    }

    public static bool GetBool(object? value, bool fallback = false)
    {
        return value switch
        {
            null => fallback,
            bool boolValue => boolValue,
            int intValue => intValue != 0,
            long longValue => longValue != 0,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            JsonElement jsonElement => GetBool(jsonElement, fallback),
            _ => fallback
        };
    }

    public static string GetString(object? value, string fallback = "")
    {
        return value switch
        {
            null => fallback,
            string stringValue => stringValue,
            JsonElement jsonElement => jsonElement.ValueKind == JsonValueKind.String
                ? jsonElement.GetString() ?? fallback
                : jsonElement.ToString(),
            _ => value.ToString() ?? fallback
        };
    }

    public static IReadOnlyList<object> GetList(object? value)
    {
        if (value is IReadOnlyList<object> list)
            return list;

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var items = new List<object>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                items.Add(item);
            }
            return items;
        }

        return Array.Empty<object>();
    }

    public static MoveRoute? GetMoveRoute(object? value)
    {
        if (value is MoveRoute route)
            return route;

        if (value is IReadOnlyList<MoveCommand> commands)
        {
            return new MoveRoute
            {
                Commands = commands.ToList()
            };
        }

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Array)
                return new MoveRoute { Commands = ParseCommands(jsonElement) };

            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                var parsedRoute = new MoveRoute
                {
                    Repeat = jsonElement.TryGetProperty("repeat", out var repeatElement) && GetBool(repeatElement, false),
                    Skippable = jsonElement.TryGetProperty("skippable", out var skippableElement) && GetBool(skippableElement, false),
                    Wait = jsonElement.TryGetProperty("wait", out var waitElement) && GetBool(waitElement, false)
                };

                if (jsonElement.TryGetProperty("list", out var listElement) && listElement.ValueKind == JsonValueKind.Array)
                {
                    parsedRoute.Commands = ParseCommands(listElement);
                }

                return parsedRoute;
            }
        }

        return null;
    }

    private static int GetInt(JsonElement element, int fallback)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed,
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => fallback
        };
    }

    private static float GetFloat(JsonElement element, float fallback)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetSingle(out var number) ? number : fallback,
            JsonValueKind.String when float.TryParse(element.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static bool GetBool(JsonElement element, bool fallback)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.TryGetInt32(out var number) && number != 0,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    private static List<MoveCommand> ParseCommands(JsonElement listElement)
    {
        var commands = new List<MoveCommand>();
        foreach (var item in listElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var command = new MoveCommand
            {
                Code = item.TryGetProperty("code", out var codeElement) ? GetInt(codeElement, 0) : 0
            };

            if (item.TryGetProperty("parameters", out var parametersElement) && parametersElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var parameter in parametersElement.EnumerateArray())
                {
                    command.Parameters.Add(parameter);
                }
            }

            commands.Add(command);
        }

        return commands;
    }
}
