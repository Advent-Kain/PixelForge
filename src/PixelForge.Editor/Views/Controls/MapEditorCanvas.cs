using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PixelForge.Editor.ViewModels;
using PixelForge.Shared.Models;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace PixelForge.Editor.Views.Controls;

public class MapEditorCanvas : Control
{
    private const int DefaultTileSize = 48;
    private readonly IBrush _emptyTileBrush = new SolidColorBrush(Color.Parse("#1A1A1A"));
    private readonly IBrush _gridBrush = new SolidColorBrush(Color.Parse("#3A3A3A"));
    private readonly IBrush _tileBrush = new SolidColorBrush(Color.Parse("#4F9CDD"));
    private readonly IBrush _layerSeparatorBrush = new SolidColorBrush(Color.Parse("#2A2A2A"));

    private bool _isPointerDown;
    private MapEditorViewModel? _viewModel;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Layers.CollectionChanged -= OnLayersCollectionChanged;
            foreach (var layer in _viewModel.Layers)
            {
                layer.VisibilityChanged -= OnLayerVisibilityChanged;
            }
        }

        _viewModel = DataContext as MapEditorViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Layers.CollectionChanged += OnLayersCollectionChanged;
            foreach (var layer in _viewModel.Layers)
            {
                layer.VisibilityChanged += OnLayerVisibilityChanged;
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (DataContext is not MapEditorViewModel viewModel || viewModel.CurrentMap == null)
        {
            DrawPlaceholder(context);
            return;
        }

        var tileSize = GetTileSize(viewModel);
        var mapWidth = viewModel.CurrentMap.Width * tileSize.Width;
        var mapHeight = viewModel.CurrentMap.Height * tileSize.Height;

        context.FillRectangle(_emptyTileBrush, new Rect(0, 0, mapWidth, mapHeight));

        foreach (var layer in viewModel.Layers.OrderBy(layer => layer.ZIndex))
        {
            if (!layer.Visible)
            {
                continue;
            }

            DrawLayer(context, layer, viewModel.CurrentMap.Width, viewModel.CurrentMap.Height, tileSize);
        }

        if (viewModel.ShowGrid)
        {
            DrawGrid(context, viewModel.CurrentMap.Width, viewModel.CurrentMap.Height, tileSize);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (DataContext is MapEditorViewModel viewModel && viewModel.CurrentMap != null)
        {
            var tileSize = GetTileSize(viewModel);
            return new Size(viewModel.CurrentMap.Width * tileSize.Width, viewModel.CurrentMap.Height * tileSize.Height);
        }

        return new Size(960, 720);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isPointerDown = true;
        HandlePointer(e.GetPosition(this));
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isPointerDown)
        {
            HandlePointer(e.GetPosition(this));
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPointerDown = false;
    }

    private void HandlePointer(Point position)
    {
        if (DataContext is not MapEditorViewModel viewModel || viewModel.CurrentMap == null)
        {
            return;
        }

        var tileSize = GetTileSize(viewModel);
        var x = (int)(position.X / tileSize.Width);
        var y = (int)(position.Y / tileSize.Height);

        if (x < 0 || y < 0 || x >= viewModel.CurrentMap.Width || y >= viewModel.CurrentMap.Height)
        {
            return;
        }

        switch (viewModel.SelectedTool)
        {
            case MapPaintTool.Erase:
                viewModel.EraseTile(x, y);
                break;
            default:
                viewModel.PaintTile(x, y);
                break;
        }

        InvalidateVisual();
    }

    private void DrawLayer(DrawingContext context, MapLayer layer, int width, int height, Size tileSize)
    {
        if (layer.Tiles.Length == 0)
        {
            return;
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var tile = layer.GetTile(x, y);
                if (tile == null || tile.IsEmpty)
                {
                    continue;
                }

                var rect = new Rect(x * tileSize.Width, y * tileSize.Height, tileSize.Width, tileSize.Height);
                context.FillRectangle(_tileBrush, rect);
                context.DrawRectangle(new Pen(_layerSeparatorBrush, 1), rect);
            }
        }
    }

    private void DrawGrid(DrawingContext context, int width, int height, Size tileSize)
    {
        var pen = new Pen(_gridBrush, 1);

        for (var x = 0; x <= width; x++)
        {
            var xPos = x * tileSize.Width;
            context.DrawLine(pen, new Point(xPos, 0), new Point(xPos, height * tileSize.Height));
        }

        for (var y = 0; y <= height; y++)
        {
            var yPos = y * tileSize.Height;
            context.DrawLine(pen, new Point(0, yPos), new Point(width * tileSize.Width, yPos));
        }
    }

    private Size GetTileSize(MapEditorViewModel viewModel)
    {
        var tileset = viewModel.SelectedTileset;
        var baseWidth = tileset?.TileWidth ?? DefaultTileSize;
        var baseHeight = tileset?.TileHeight ?? DefaultTileSize;
        return new Size(
            Math.Max(4, baseWidth * viewModel.ZoomLevel),
            Math.Max(4, baseHeight * viewModel.ZoomLevel));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnLayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (MapLayer layer in e.OldItems)
            {
                layer.VisibilityChanged -= OnLayerVisibilityChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (MapLayer layer in e.NewItems)
            {
                layer.VisibilityChanged += OnLayerVisibilityChanged;
            }
        }

        InvalidateVisual();
    }

    private void OnLayerVisibilityChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void DrawPlaceholder(DrawingContext context)
    {
        var text = new FormattedText(
            "Map Editor Canvas",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            24,
            Brushes.Gray);

        context.DrawText(text, new Point(10, 10));
    }
}
