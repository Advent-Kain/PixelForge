using PixelForge.Engine.Core;
using PixelForge.Engine.Events;
using System.Reflection;
using System.Runtime.Loader;

namespace PixelForge.Engine.Scripting;

/// <summary>
/// Runtime loader for compiled script assemblies.
/// </summary>
public sealed class ScriptRuntime
{
    private readonly List<Assembly> _assemblies = new();
    private readonly List<Type> _scriptTypes = new();

    private ScriptRuntime()
    {
    }

    public static ScriptRuntime Instance { get; } = new();

    public IReadOnlyList<Type> ScriptTypes => _scriptTypes;

    public void Clear()
    {
        _assemblies.Clear();
        _scriptTypes.Clear();
    }

    public void LoadAssembly(byte[] assemblyBytes, byte[]? pdbBytes = null)
    {
        using var assemblyStream = new MemoryStream(assemblyBytes);
        using var pdbStream = pdbBytes != null ? new MemoryStream(pdbBytes) : null;
        var assembly = pdbStream != null
            ? AssemblyLoadContext.Default.LoadFromStream(assemblyStream, pdbStream)
            : AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        RegisterAssembly(assembly);
    }

    public void LoadAssemblyFromPath(string assemblyPath)
    {
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        RegisterAssembly(assembly);
    }

    public GameScript? CreateScript(string typeName, IGameContext gameContext)
    {
        var scriptType = _scriptTypes.FirstOrDefault(type =>
            string.Equals(type.FullName, typeName, StringComparison.Ordinal) ||
            string.Equals(type.Name, typeName, StringComparison.Ordinal));

        if (scriptType == null)
            return null;

        if (Activator.CreateInstance(scriptType) is not GameScript script)
            return null;

        script.Attach(gameContext);
        return script;
    }

    public bool TryExecuteScript(string typeName, EventContext context)
    {
        var script = CreateScript(typeName, context.Game);
        if (script == null)
            return false;

        script.Execute(context);
        return true;
    }

    private void RegisterAssembly(Assembly assembly)
    {
        if (_assemblies.Contains(assembly))
            return;

        _assemblies.Add(assembly);
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract)
                continue;

            if (typeof(GameScript).IsAssignableFrom(type))
                _scriptTypes.Add(type);
        }
    }
}
