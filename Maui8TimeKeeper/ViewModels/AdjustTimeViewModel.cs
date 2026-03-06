using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui8TimeKeeper.Models;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Maui8TimeKeeper.ViewModels;

public partial class AdjustTimeViewModel : ObservableObject
{
    private readonly TimeCardService _timeCardService;
    private readonly Dictionary<string, int> _dragAppliedMinutes = [];
    private DateTime _windowStartLocal;
    private DateTime _windowEndLocal;
    private double _viewportTimelineWidth = 1;

    public AdjustTimeViewModel(TimeCardService timeCardService)
    {
        _timeCardService = timeCardService;
        selectedDate = DateTime.Today;
        _windowStartLocal = SelectedDate.Date;
        _windowEndLocal = SelectedDate.Date.AddDays(1);
        HourMarkers = [];
        Rows = [];
        TimelineWidth = 24 * BaseHourWidth;
        LoadTimeline();
    }

    public const double BaseHourWidth = 120;

    public const double LeftColumnWidth = 130;
    public const double HeaderHeight = 34;
    public const double RowHeight = 44;

    public ObservableCollection<HourMarkerViewModel> HourMarkers { get; }

    public ObservableCollection<TimelineRowViewModel> Rows { get; }

    [ObservableProperty]
    private DateTime selectedDate;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private double timelineWidth;

    [ObservableProperty]
    private double zoomFactor = 1.25;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PixelsPerMinute))]
    private double visibleMinutes = 24d * 60d;

    [ObservableProperty]
    private double panOffsetPixels;

    [ObservableProperty]
    private double canvasHeight = HeaderHeight + 12;

    [ObservableProperty]
    private string visibleRangeLabel = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModeLabel))]
    private bool editMode = true;

    public string ModeLabel => EditMode ? "Editing Handles" : "Panning Timeline";

    public double PixelsPerMinute => TimelineWidth / Math.Max(1, VisibleMinutes);

    partial void OnSelectedDateChanged(DateTime value)
    {
        LoadTimeline();
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomAround(Math.Min(8.0, ZoomFactor + 0.5), LeftColumnWidth + (_viewportTimelineWidth / 2d));
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomAround(Math.Max(0.5, ZoomFactor - 0.5), LeftColumnWidth + (_viewportTimelineWidth / 2d));
    }

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomAround(1.0, LeftColumnWidth + (_viewportTimelineWidth / 2d));
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        EditMode = !EditMode;
    }

    public void BeginDrag(TimelineHandleViewModel? handle)
    {
        if (handle == null)
        {
            return;
        }

        _dragAppliedMinutes[handle.Key] = 0;
    }

    public void BeginDrag(TimelineSegmentViewModel? segment, bool isStartHandle)
    {
        if (segment == null)
        {
            return;
        }

        _dragAppliedMinutes[BuildDragKey(segment, isStartHandle)] = 0;
    }

    public bool ApplyDrag(TimelineHandleViewModel? handle, int totalDeltaMinutes)
    {
        if (handle == null)
        {
            return false;
        }

        _dragAppliedMinutes.TryGetValue(handle.Key, out var alreadyApplied);
        var step = totalDeltaMinutes - alreadyApplied;
        if (step == 0)
        {
            return true;
        }

        var success = _timeCardService.TryAdjustSegmentBoundary(handle.Segment.Source, handle.IsStartHandle, step);
        if (!success)
        {
            return false;
        }

        _dragAppliedMinutes[handle.Key] = totalDeltaMinutes;
        LoadTimeline();
        return true;
    }

    public bool ApplyDrag(TimelineSegmentViewModel? segment, bool isStartHandle, int totalDeltaMinutes)
    {
        if (segment == null)
        {
            return false;
        }

        var key = BuildDragKey(segment, isStartHandle);
        _dragAppliedMinutes.TryGetValue(key, out var alreadyApplied);
        var step = totalDeltaMinutes - alreadyApplied;
        if (step == 0)
        {
            return true;
        }

        var success = _timeCardService.TryAdjustSegmentBoundary(segment.Source, isStartHandle, step);
        if (!success)
        {
            return false;
        }

        _dragAppliedMinutes[key] = totalDeltaMinutes;
        LoadTimeline();
        return true;
    }

    public async Task EndDrag(TimelineHandleViewModel? handle)
    {
        if (handle == null)
        {
            return;
        }

        _dragAppliedMinutes.Remove(handle.Key);
        await _timeCardService.Save();
        StatusMessage = "Timeline updated.";
    }

    public async Task EndDrag(TimelineSegmentViewModel? segment, bool isStartHandle)
    {
        if (segment == null)
        {
            return;
        }

        _dragAppliedMinutes.Remove(BuildDragKey(segment, isStartHandle));
        await _timeCardService.Save();
        LoadTimeline();
        StatusMessage = "Timeline updated.";
    }

    public async Task<bool> NudgeBoundary(TimelineSegmentViewModel? segment, bool isStartHandle, int deltaMinutes)
    {
        if (segment == null || deltaMinutes == 0)
        {
            return false;
        }

        var success = _timeCardService.TryAdjustSegmentBoundary(segment.Source, isStartHandle, deltaMinutes);
        if (!success)
        {
            StatusMessage = "Move rejected: segment would become invalid.";
            return false;
        }

        await _timeCardService.Save();
        LoadTimeline();
        StatusMessage = "Boundary updated.";
        return true;
    }

    public void LoadTimeline()
    {
        var allSegments = _timeCardService.GetEditableTimelineSegments(SelectedDate)
            .OrderBy(x => x.Card.Name)
            .ThenBy(x => x.StartLocal)
            .ToList();

        UpdateVisibleWindow(allSegments);
        BuildHourMarkers();

        var rows = allSegments
            .GroupBy(x => x.Card.Id)
            .Select(g => new TimelineRowViewModel
            {
                CardId = g.Key,
                RowTitle = g.First().Card.Name,
                ChargeCode = g.First().Card.ChargeCode,
                Segments = g.OrderBy(x => x.StartLocal)
                    .Select(x => TimelineSegmentViewModel.Create(x, _windowStartLocal, PixelsPerMinute))
                    .ToList()
            })
            .OrderBy(x => x.RowTitle)
            .ToList();

        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        CanvasHeight = HeaderHeight + (Rows.Count * RowHeight) + 12;
        PanOffsetPixels = Math.Clamp(PanOffsetPixels, 0, MaxPanOffsetPixels);

        StatusMessage = rows.Count == 0
            ? "No tracked segments for this day."
            : "Drag a segment edge to move a boundary.";
    }

    public double MaxPanOffsetPixels => Math.Max(0, TimelineWidth - _viewportTimelineWidth);

    public void UpdateViewportWidth(double fullCanvasWidth)
    {
        _viewportTimelineWidth = Math.Max(1, fullCanvasWidth - LeftColumnWidth);
        PanOffsetPixels = Math.Clamp(PanOffsetPixels, 0, MaxPanOffsetPixels);
    }

    public void PanByPixels(double deltaX)
    {
        if (EditMode)
        {
            return;
        }

        PanOffsetPixels = Math.Clamp(PanOffsetPixels - deltaX, 0, MaxPanOffsetPixels);
    }

    public void ZoomAround(double targetZoomFactor, double anchorCanvasX)
    {
        var clampedZoom = Math.Clamp(targetZoomFactor, 0.5, 8.0);
        if (Math.Abs(clampedZoom - ZoomFactor) < 0.0001)
        {
            return;
        }

        var oldPixelsPerMinute = Math.Max(0.0001, PixelsPerMinute);
        var viewportX = Math.Clamp(anchorCanvasX - LeftColumnWidth, 0, _viewportTimelineWidth);
        var anchorMinute = (PanOffsetPixels + viewportX) / oldPixelsPerMinute;

        ZoomFactor = clampedZoom;
        LoadTimeline();

        var newWorldX = anchorMinute * PixelsPerMinute;
        PanOffsetPixels = Math.Clamp(newWorldX - viewportX, 0, MaxPanOffsetPixels);
    }

    private void UpdateVisibleWindow(List<TimelineEditableSegment> segments)
    {
        var dayStart = SelectedDate.Date;
        var dayEnd = dayStart.AddDays(1);

        if (segments.Count == 0)
        {
            _windowStartLocal = dayStart;
            _windowEndLocal = dayEnd;
        }
        else
        {
            var first = segments.Min(x => x.StartLocal).AddMinutes(-5);
            var last = segments.Max(x => x.EndLocal).AddMinutes(5);
            _windowStartLocal = first < dayStart ? dayStart : first;
            _windowEndLocal = last > dayEnd ? dayEnd : last;

            if (_windowEndLocal <= _windowStartLocal)
            {
                _windowStartLocal = dayStart;
                _windowEndLocal = dayEnd;
            }
        }

        VisibleMinutes = (_windowEndLocal - _windowStartLocal).TotalMinutes;
        var visibleHours = VisibleMinutes / 60d;
        TimelineWidth = Math.Max(480, visibleHours * BaseHourWidth * ZoomFactor);
        VisibleRangeLabel = $"{_windowStartLocal:HH:mm} - {_windowEndLocal:HH:mm}";
    }

    private static string BuildDragKey(TimelineSegmentViewModel segment, bool isStartHandle)
    {
        var suffix = isStartHandle ? "s" : "e";
        var durationIdentity = RuntimeHelpers.GetHashCode(segment.Source.Duration);
        return $"{segment.Source.Card.Id}_{durationIdentity}_{suffix}";
    }

    private void BuildHourMarkers()
    {
        HourMarkers.Clear();
        var startHour = new DateTime(_windowStartLocal.Year, _windowStartLocal.Month, _windowStartLocal.Day, _windowStartLocal.Hour, 0, 0);
        if (startHour < _windowStartLocal)
        {
            startHour = startHour.AddHours(1);
        }

        var firstMarker = _windowStartLocal;
        HourMarkers.Add(HourMarkerViewModel.Create(firstMarker, _windowStartLocal, PixelsPerMinute, true));

        for (var marker = startHour; marker < _windowEndLocal; marker = marker.AddHours(1))
        {
            HourMarkers.Add(HourMarkerViewModel.Create(marker, _windowStartLocal, PixelsPerMinute, false));
        }

        HourMarkers.Add(HourMarkerViewModel.Create(_windowEndLocal, _windowStartLocal, PixelsPerMinute, true));
    }
}

public sealed class HourMarkerViewModel
{
    public HourMarkerViewModel(string label, double x)
    {
        Label = label;
        X = x;
        HeaderBounds = new Rect(x, 0, 52, 34);
        GridLineBounds = new Rect(x, 0, 1, 40);
    }

    public string Label { get; }

    public double X { get; }

    public Rect HeaderBounds { get; }

    public Rect GridLineBounds { get; }

    public static HourMarkerViewModel Create(DateTime value, DateTime windowStart, double pixelsPerMinute, bool showMinute)
    {
        var x = Math.Max(0, (value - windowStart).TotalMinutes * pixelsPerMinute);
        var label = showMinute ? value.ToString("HH:mm") : $"{value:HH}:00";
        return new HourMarkerViewModel(label, x);
    }
}

public sealed class TimelineRowViewModel
{
    public required Guid CardId { get; init; }

    public required string RowTitle { get; init; }

    public required string ChargeCode { get; init; }

    public required List<TimelineSegmentViewModel> Segments { get; init; }

    public string Subtitle => string.IsNullOrWhiteSpace(ChargeCode) ? "(No charge code)" : ChargeCode;
}

public sealed class TimelineSegmentViewModel
{
    public required TimelineEditableSegment Source { get; init; }

    public required string FillColor { get; init; }

    public required Rect Bounds { get; init; }

    public required string Label { get; init; }

    public required TimelineHandleViewModel StartHandle { get; init; }

    public required TimelineHandleViewModel EndHandle { get; init; }

    public static TimelineSegmentViewModel Create(TimelineEditableSegment source, DateTime windowStartLocal, double pixelsPerMinute)
    {
        var startMinutes = (source.StartLocal - windowStartLocal).TotalMinutes;
        var durationMinutes = Math.Max(1, (source.EndLocal - source.StartLocal).TotalMinutes);
        var x = Math.Max(0, startMinutes * pixelsPerMinute);
        var width = Math.Max(10, durationMinutes * pixelsPerMinute);

        var segmentVm = new TimelineSegmentViewModel
        {
            Source = source,
            FillColor = ColorFor(source.Card.Id),
            Bounds = new Rect(x, 6, width, 28),
            Label = $"{source.StartLocal:HH:mm}-{source.EndLocal:HH:mm}",
            StartHandle = new TimelineHandleViewModel($"{source.Card.Id}_{source.Duration.StartTime.Ticks}_s", true),
            EndHandle = new TimelineHandleViewModel($"{source.Card.Id}_{source.Duration.StartTime.Ticks}_e", false)
        };

        segmentVm.StartHandle.Segment = segmentVm;
        segmentVm.EndHandle.Segment = segmentVm;
        return segmentVm;
    }

    private static string ColorFor(Guid id)
    {
        var hash = Math.Abs(id.GetHashCode());
        var r = 80 + (hash & 0x5F);
        var g = 80 + ((hash >> 6) & 0x5F);
        var b = 80 + ((hash >> 12) & 0x5F);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}

public sealed class TimelineHandleViewModel
{
    public TimelineHandleViewModel(string key, bool isStartHandle)
    {
        Key = key;
        IsStartHandle = isStartHandle;
    }

    public string Key { get; }

    public bool IsStartHandle { get; }

    public TimelineSegmentViewModel Segment { get; set; } = null!;
}
