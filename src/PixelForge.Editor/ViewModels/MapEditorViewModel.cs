using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelForge.Shared.Models;
using System.Collections.ObjectModel;

namespace PixelForge.Editor.ViewModels;

/// <summary>
/// View model for the map editor.
/// </summary>
public partial class MapEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    private MapData? _currentMap;

    [ObservableProperty]
    private MapLayer? _selectedLayer;

    [ObservableProperty]
    private int _selectedTileId = -1;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private bool _showEvents = true;

    public ObservableCollection<MapLayer> Layers { get; } = new();

    public MapEditorViewModel()
    {
        // Create a default map
        NewMap();
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
        if (SelectedLayer == null || SelectedTileId < 0)
            return;

        var tile = new TileData
        {
            TilesetId = 0,
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
}
