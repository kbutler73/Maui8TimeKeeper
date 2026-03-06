using Maui8TimeKeeper.ViewModels;
using Microsoft.Maui.Graphics;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Maui8TimeKeeper.Views;

public partial class AdjustTimeView : ContentPage
{
    private readonly AdjustTimelineDrawable _drawable;
    private TimelineSegmentViewModel? _activeSegment;
    private bool _activeIsStartHandle;
    private bool _isDraggingHandle;
    private bool _isPanning;
    private float _dragStartX;
    private PointF _lastPanPoint;
    private double _pinchStartZoom;
    private bool _subscriptionsAttached;
    private double _lastPanTotalX;

    private const float HandleHitWidth = 42f;

    public AdjustTimeView(AdjustTimeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        _drawable = new AdjustTimelineDrawable(viewModel);
        TimelineCanvas.Drawable = _drawable;
        AttachSubscriptions();
    }

    private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        TimelineCanvas.Invalidate();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        if (e.PropertyName is nameof(AdjustTimeViewModel.TimelineWidth) or nameof(AdjustTimeViewModel.PanOffsetPixels))
        {
            vm.UpdateViewportWidth(TimelineCanvas.Width);
        }

        TimelineCanvas.Invalidate();
    }

    private void OnTimelineCanvasSizeChanged(object? sender, EventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        vm.UpdateViewportWidth(TimelineCanvas.Width);
        TimelineCanvas.Invalidate();
    }

    private void OnTimelineStartInteraction(object? sender, TouchEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm || e.Touches.Length == 0)
        {
            return;
        }

        var point = e.Touches[0];
        if (vm.EditMode && TryHitHandle(point, out var segment, out var isStartHandle))
        {
            _activeSegment = segment;
            _activeIsStartHandle = isStartHandle;
            _isDraggingHandle = true;
            _dragStartX = point.X;
            vm.BeginDrag(segment, isStartHandle);
            return;
        }

        _isDraggingHandle = false;
        _activeSegment = null;
        _isPanning = true;
        _lastPanPoint = point;
    }

    private void OnTimelineDragInteraction(object? sender, TouchEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm || e.Touches.Length == 0)
        {
            return;
        }

        var point = e.Touches[0];
        if (_isDraggingHandle && _activeSegment != null)
        {
            var deltaX = point.X - _dragStartX;
            var minutes = (int)Math.Round(deltaX / vm.PixelsPerMinute, MidpointRounding.AwayFromZero);
            if (vm.ApplyDrag(_activeSegment, _activeIsStartHandle, minutes))
            {
                TimelineCanvas.Invalidate();
            }

            return;
        }

        if (_isPanning)
        {
            var deltaX = point.X - _lastPanPoint.X;
            vm.PanByPixels(deltaX);
            _lastPanPoint = point;
            TimelineCanvas.Invalidate();
        }
    }

    private async void OnTimelineEndInteraction(object? sender, TouchEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        if (_isDraggingHandle && _activeSegment != null)
        {
            await vm.EndDrag(_activeSegment, _activeIsStartHandle);
        }

        _activeSegment = null;
        _isDraggingHandle = false;
        _isPanning = false;
        TimelineCanvas.Invalidate();
    }

    private void OnTimelinePinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm || TimelineCanvas.Width <= 0)
        {
            return;
        }

        switch (e.Status)
        {
            case GestureStatus.Started:
                _pinchStartZoom = vm.ZoomFactor;
                break;
            case GestureStatus.Running:
                var targetZoom = _pinchStartZoom * e.Scale;
                var anchorX = e.ScaleOrigin.X * TimelineCanvas.Width;
                vm.ZoomAround(targetZoom, anchorX);
                TimelineCanvas.Invalidate();
                break;
        }
    }

    private void OnTimelineTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm || !vm.EditMode)
        {
            return;
        }

        var position = e.GetPosition(TimelineCanvas);
        if (position is null)
        {
            return;
        }

        if (TryHitHandle(new PointF((float)position.Value.X, (float)position.Value.Y), out var segment, out var isStartHandle))
        {
            _activeSegment = segment;
            _activeIsStartHandle = isStartHandle;
            _isDraggingHandle = true;
            _dragStartX = (float)position.Value.X;
            vm.BeginDrag(segment, isStartHandle);
        }
    }

    private void OnTimelinePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastPanTotalX = 0;
                break;
            case GestureStatus.Running:
                if (_isDraggingHandle && _activeSegment != null)
                {
                    var minutes = (int)Math.Round(e.TotalX / vm.PixelsPerMinute, MidpointRounding.AwayFromZero);
                    if (vm.ApplyDrag(_activeSegment, _activeIsStartHandle, minutes))
                    {
                        TimelineCanvas.Invalidate();
                    }
                }
                else if (!vm.EditMode)
                {
                    var delta = e.TotalX - _lastPanTotalX;
                    _lastPanTotalX = e.TotalX;
                    vm.PanByPixels(delta);
                    TimelineCanvas.Invalidate();
                }

                break;
            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                _lastPanTotalX = 0;
                if (_isDraggingHandle && _activeSegment != null)
                {
                    _ = vm.EndDrag(_activeSegment, _activeIsStartHandle);
                }

                _activeSegment = null;
                _isDraggingHandle = false;
                break;
        }
    }

    private void OnTimelinePointerPressed(object? sender, PointerEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        var pos = e.GetPosition(TimelineCanvas);
        if (pos is null)
        {
            return;
        }

        var point = new PointF((float)pos.Value.X, (float)pos.Value.Y);
        if (vm.EditMode && TryHitHandle(point, out var segment, out var isStartHandle))
        {
            _activeSegment = segment;
            _activeIsStartHandle = isStartHandle;
            _isDraggingHandle = true;
            _dragStartX = point.X;
            vm.BeginDrag(segment, isStartHandle);
            return;
        }

        _isPanning = true;
        _lastPanPoint = point;
    }

    private void OnTimelinePointerMoved(object? sender, PointerEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        var pos = e.GetPosition(TimelineCanvas);
        if (pos is null)
        {
            return;
        }

        var point = new PointF((float)pos.Value.X, (float)pos.Value.Y);
        if (_isDraggingHandle && _activeSegment != null)
        {
            var deltaX = point.X - _dragStartX;
            var minutes = (int)Math.Round(deltaX / vm.PixelsPerMinute, MidpointRounding.AwayFromZero);
            if (vm.ApplyDrag(_activeSegment, _activeIsStartHandle, minutes))
            {
                TimelineCanvas.Invalidate();
            }

            return;
        }

        if (_isPanning && !vm.EditMode)
        {
            var delta = point.X - _lastPanPoint.X;
            vm.PanByPixels(delta);
            _lastPanPoint = point;
            TimelineCanvas.Invalidate();
        }
    }

    private void OnTimelinePointerReleased(object? sender, PointerEventArgs e)
    {
        if (BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        if (_isDraggingHandle && _activeSegment != null)
        {
            _ = vm.EndDrag(_activeSegment, _activeIsStartHandle);
        }

        _activeSegment = null;
        _isDraggingHandle = false;
        _isPanning = false;
    }

    private bool TryHitHandle(PointF point, out TimelineSegmentViewModel? segment, out bool isStartHandle)
    {
        segment = null;
        isStartHandle = false;

        if (BindingContext is not AdjustTimeViewModel vm)
        {
            return false;
        }

        var timelineTop = (float)AdjustTimeViewModel.HeaderHeight;
        if (point.Y < timelineTop - 12)
        {
            return false;
        }

        var worldX = point.X - (float)AdjustTimeViewModel.LeftColumnWidth + (float)vm.PanOffsetPixels;
        if (worldX < -40 || worldX > vm.TimelineWidth + 40)
        {
            return false;
        }

        var bestDistance = double.MaxValue;
        for (var rowIndex = 0; rowIndex < vm.Rows.Count; rowIndex++)
        {
            var row = vm.Rows[rowIndex];
            foreach (var candidate in row.Segments)
            {
                var segmentTop = timelineTop + (float)(rowIndex * AdjustTimeViewModel.RowHeight) + (float)candidate.Bounds.Y;
                var segmentBottom = segmentTop + (float)candidate.Bounds.Height;
                if (point.Y < segmentTop - 18 || point.Y > segmentBottom + 18)
                {
                    continue;
                }

                var leftEdge = candidate.Bounds.X;
                var rightEdge = candidate.Bounds.X + candidate.Bounds.Width;
                var leftDistance = Math.Abs(worldX - leftEdge);
                var rightDistance = Math.Abs(worldX - rightEdge);
                var insideSegment = worldX >= leftEdge && worldX <= rightEdge;

                if (insideSegment)
                {
                    if (leftDistance <= rightDistance && leftDistance < bestDistance)
                    {
                        bestDistance = leftDistance;
                        segment = candidate;
                        isStartHandle = true;
                    }
                    else if (rightDistance < bestDistance)
                    {
                        bestDistance = rightDistance;
                        segment = candidate;
                        isStartHandle = false;
                    }

                    continue;
                }

                if (leftDistance <= HandleHitWidth && leftDistance < bestDistance)
                {
                    bestDistance = leftDistance;
                    segment = candidate;
                    isStartHandle = true;
                }

                if (rightDistance <= HandleHitWidth && rightDistance < bestDistance)
                {
                    bestDistance = rightDistance;
                    segment = candidate;
                    isStartHandle = false;
                }
            }
        }

        if (segment != null)
        {
            vm.StatusMessage = isStartHandle ? "Start handle selected." : "End handle selected.";
        }

        return segment != null;
    }

    protected override void OnDisappearing()
    {
        DetachSubscriptions();
        base.OnDisappearing();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AttachSubscriptions();
        TimelineCanvas.Invalidate();
    }

    private void AttachSubscriptions()
    {
        if (_subscriptionsAttached || BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        vm.PropertyChanged += OnViewModelPropertyChanged;
        vm.Rows.CollectionChanged += OnRowsCollectionChanged;
        _subscriptionsAttached = true;
    }

    private void DetachSubscriptions()
    {
        if (!_subscriptionsAttached || BindingContext is not AdjustTimeViewModel vm)
        {
            return;
        }

        vm.PropertyChanged -= OnViewModelPropertyChanged;
        vm.Rows.CollectionChanged -= OnRowsCollectionChanged;
        _subscriptionsAttached = false;
    }
}

internal sealed class AdjustTimelineDrawable(AdjustTimeViewModel viewModel) : IDrawable
{
    private readonly AdjustTimeViewModel _viewModel = viewModel;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        _viewModel.UpdateViewportWidth(dirtyRect.Width);

        var isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
        var background = isDarkMode ? Color.FromArgb("#1F2937") : Color.FromArgb("#F8FAFC");
        var border = isDarkMode ? Color.FromArgb("#4B5563") : Color.FromArgb("#CBD5E1");
        var text = isDarkMode ? Colors.White : Colors.Black;
        var subtitle = isDarkMode ? Color.FromArgb("#D1D5DB") : Color.FromArgb("#4B5563");
        var handleColor = isDarkMode ? Color.FromArgb("#E5E7EB") : Color.FromArgb("#111827");

        canvas.FillColor = background;
        canvas.FillRectangle(dirtyRect);

        DrawHeader(canvas, border, text);
        DrawRows(canvas, border, text, subtitle, handleColor);
    }

    private void DrawHeader(ICanvas canvas, Color border, Color text)
    {
        var headerHeight = (float)AdjustTimeViewModel.HeaderHeight;
        var left = (float)AdjustTimeViewModel.LeftColumnWidth;
        var pan = (float)_viewModel.PanOffsetPixels;

        canvas.StrokeColor = border;
        canvas.StrokeSize = 1;
        canvas.DrawLine(left, 0, left, headerHeight);
        canvas.DrawLine(0, headerHeight, (float)(left + _viewModel.TimelineWidth), headerHeight);

        canvas.FontColor = text;
        canvas.FontSize = 10;

        foreach (var marker in _viewModel.HourMarkers)
        {
            var x = left + (float)marker.X - pan;
            if (x < left - 60 || x > left + (float)_viewModel.TimelineWidth + 60)
            {
                continue;
            }

            canvas.StrokeColor = border;
            canvas.DrawLine(x, 0, x, headerHeight);
            canvas.DrawString(marker.Label, x + 3, 0, 56, headerHeight, HorizontalAlignment.Left, VerticalAlignment.Center);
        }
    }

    private void DrawRows(ICanvas canvas, Color border, Color text, Color subtitle, Color handleColor)
    {
        var headerHeight = (float)AdjustTimeViewModel.HeaderHeight;
        var rowHeight = (float)AdjustTimeViewModel.RowHeight;
        var left = (float)AdjustTimeViewModel.LeftColumnWidth;
        var pan = (float)_viewModel.PanOffsetPixels;

        for (var rowIndex = 0; rowIndex < _viewModel.Rows.Count; rowIndex++)
        {
            var row = _viewModel.Rows[rowIndex];
            var rowTop = headerHeight + (rowIndex * rowHeight);

            canvas.StrokeColor = border;
            canvas.StrokeSize = 1;
            canvas.DrawLine(0, rowTop, left + (float)_viewModel.TimelineWidth, rowTop);
            canvas.DrawLine(left, rowTop, left, rowTop + rowHeight);

            canvas.FontColor = text;
            canvas.FontSize = 13;
            canvas.DrawString(row.RowTitle, 8, rowTop + 3, left - 12, 18, HorizontalAlignment.Left, VerticalAlignment.Center);

            canvas.FontColor = subtitle;
            canvas.FontSize = 10;
            canvas.DrawString(row.Subtitle, 8, rowTop + 20, left - 12, 14, HorizontalAlignment.Left, VerticalAlignment.Center);

            foreach (var segment in row.Segments)
            {
                var x = left + (float)segment.Bounds.X - pan;
                var y = rowTop + (float)segment.Bounds.Y;
                var width = (float)segment.Bounds.Width;
                var height = (float)segment.Bounds.Height;

                if (x + width < left - 40 || x > left + (float)_viewModel.TimelineWidth + 40)
                {
                    continue;
                }

                canvas.FillColor = Color.FromArgb(segment.FillColor);
                canvas.FillRoundedRectangle(x, y, width, height, 4);
                canvas.StrokeColor = border;
                canvas.DrawRoundedRectangle(x, y, width, height, 4);

                canvas.FontColor = Colors.White;
                canvas.FontSize = 10;
                canvas.DrawString(segment.Label, x + 10, y, Math.Max(0, width - 20), height, HorizontalAlignment.Left, VerticalAlignment.Center);

                if (_viewModel.EditMode)
                {
                    var handleTop = y + 2;
                    var handleHeight = height - 4;
                    canvas.FillColor = handleColor;
                    canvas.FillRoundedRectangle(x, handleTop, 4, handleHeight, 2);
                    canvas.FillRoundedRectangle(x + width - 4, handleTop, 4, handleHeight, 2);
                }
            }
        }
    }
}
