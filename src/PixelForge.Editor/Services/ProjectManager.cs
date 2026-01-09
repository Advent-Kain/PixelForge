using PixelForge.Shared.Models;
using System.IO;

namespace PixelForge.Editor.Services;

public static class ProjectManager
{
    public const string ProjectFileName = "project.json";

    public static ProjectFile? CurrentProject { get; private set; }
    public static string? ProjectFilePath { get; private set; }

    public static string? ProjectRoot
        => ProjectFilePath == null ? null : Path.GetDirectoryName(ProjectFilePath);

    public static bool HasProject => CurrentProject != null && ProjectFilePath != null;

    public static void SetProject(ProjectFile projectFile, string projectFilePath)
    {
        CurrentProject = projectFile;
        ProjectFilePath = projectFilePath;
    }

    public static ProjectFile CreateDefaultProject(string projectName)
    {
        return new ProjectFile
        {
            Name = projectName,
            MapsPath = "Maps",
            DatabasePath = "Database",
            AssetsPath = "Assets",
            DefaultMap = Path.Combine("Maps", "Map001.json")
        };
    }

    public static string? GetRuntimeProjectPath()
    {
        var runtimePath = CurrentProject?.RuntimeProjectPath;
        if (string.IsNullOrWhiteSpace(runtimePath))
            return null;

        var trimmedPath = runtimePath.Trim();
        if (Path.IsPathRooted(trimmedPath))
            return trimmedPath;

        if (ProjectRoot == null)
            return trimmedPath;

        return Path.Combine(ProjectRoot, trimmedPath);
    }

    public static string? GetMapsDirectory()
    {
        if (!HasProject)
            return null;

        return Path.Combine(ProjectRoot!, CurrentProject!.MapsPath);
    }

    public static string? GetDatabaseDirectory()
    {
        if (!HasProject)
            return null;

        return Path.Combine(ProjectRoot!, CurrentProject!.DatabasePath);
    }

    public static string? GetAssetsDirectory()
    {
        if (!HasProject)
            return null;

        return Path.Combine(ProjectRoot!, CurrentProject!.AssetsPath);
    }

    public static string? GetDefaultMapPath()
    {
        if (!HasProject)
            return null;

        return Path.Combine(ProjectRoot!, CurrentProject!.DefaultMap);
    }
}
