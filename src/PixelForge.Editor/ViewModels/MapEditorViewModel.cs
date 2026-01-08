using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Editor.Services;
using PixelForge.Shared.Models;
using PixelForge.Shared.Models.Database;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for the map editor.
/// </summary>
public partial class MapEditorViewModel : ViewModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [ObservableProperty]
    private MapData? _currentMap;

    [ObservableProperty]
    private MapLayer? _selectedLayer;

    [ObservableProperty]
    private int _selectedTileId = -1;

    [ObservableProperty]
    private Tileset? _selectedTileset;

    [ObservableProperty]
    private MapPaintTool _selectedTool = MapPaintTool.Paint;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private bool _showEvents = true;

    public ObservableCollection<MapLayer> Layers { get; } = new();
    public ObservableCollection<Tileset> Tilesets { get; } = new();
    public ObservableCollection<int> TilePalette { get; } = new();
    public ObservableCollection<MapPaintTool> PaintTools { get; } = new(Enum.GetValues<MapPaintTool>());

    public MapEditorViewModel()
    {
        // Create a default map
        NewMap();
        LoadTilesets();
    }

    [RelayCommand]
    private void NewMap()
    {
        CurrentMap = MapData.CreateDefault(20, 15);
        Layers.Clear();

        foreach (var layer in CurrentMap.Layers)
        {
            Layers.Add(layer);
        }

        SelectedLayer = Layers.FirstOrDefault();
    }

    [RelayCommand]
    private void AddLayer()
    {
        if (CurrentMap == null)
            return;

        var newLayer = new MapLayer
        {
            Name = $"Layer {Layers.Count + 1}",
            Type = LayerType.Tile,
            ZIndex = Layers.Count
        };
        newLayer.Initialize(CurrentMap.Width, CurrentMap.Height);

        CurrentMap.Layers.Add(newLayer);
        Layers.Add(newLayer);
        SelectedLayer = newLayer;
    }

    [RelayCommand]
    private void RemoveLayer()
    {
        if (SelectedLayer == null || CurrentMap == null)
            return;

        CurrentMap.Layers.Remove(SelectedLayer);
        Layers.Remove(SelectedLayer);
        SelectedLayer = Layers.FirstOrDefault();
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + 0.25, 4.0);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - 0.25, 0.25);
    }

    [RelayCommand]
    private void ToggleGrid()
    {
        ShowGrid = !ShowGrid;
    }

    [RelayCommand]
    private void ToggleEvents()
    {
        ShowEvents = !ShowEvents;
    }

    /// <summary>
    /// Paint a tile at the specified position.
    /// </summary>
    public void PaintTile(int x, int y)
    {
        if (SelectedLayer == null || SelectedTileId < 0 || SelectedTileset == null)
            return;

        var tile = new TileData
        {
            TilesetId = SelectedTileset.Id,
            TileId = SelectedTileId
        };

        SelectedLayer.SetTile(x, y, tile);
    }

    /// <summary>
    /// Erase a tile at the specified position.
    /// </summary>
    public void EraseTile(int x, int y)
    {
        if (SelectedLayer == null)
            return;

        SelectedLayer.SetTile(x, y, TileData.Empty);
    }

    public void LoadMap(MapData map)
    {
        CurrentMap = map;
        Layers.Clear();

        foreach (var layer in map.Layers)
        {
            Layers.Add(layer);
        }

        SelectedLayer = Layers.FirstOrDefault();
    }

    partial void OnSelectedTilesetChanged(Tileset? value)
    {
        BuildTilePalette(value);
    }

    private void BuildTilePalette(Tileset? tileset)
    {
        TilePalette.Clear();

        if (tileset == null)
        {
            SelectedTileId = -1;
            return;
        }

        int count = Math.Max(0, tileset.Columns * tileset.Rows);
        for (int i = 0; i < count; i++)
        {
            TilePalette.Add(i);
        }

        SelectedTileId = TilePalette.Count > 0 ? TilePalette[0] : -1;
    }

    private void LoadTilesets()
    {
        Tilesets.Clear();
        var databaseDirectory = ProjectManager.GetDatabaseDirectory();
        if (databaseDirectory != null)
        {
            var filePath = Path.Combine(databaseDirectory, "tilesets.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var tilesets = JsonSerializer.Deserialize<List<Tileset>>(json, JsonOptions) ?? new List<Tileset>();
                foreach (var tileset in tilesets)
                {
                    Tilesets.Add(tileset);
                }
            }
        }

        if (Tilesets.Count == 0)
        {
            Tilesets.Add(new Tileset
            {
                Id = 1,
                Name = "Default Tileset",
                ImagePath = "Assets/Graphics/Tilesets/Default.png",
                TileWidth = 48,
                TileHeight = 48,
                Columns = 8,
                Rows = 8
            });
        }

        SelectedTileset = Tilesets.FirstOrDefault();
    }
}

public enum MapPaintTool
{
    Paint,
    Erase,
    Fill
}
