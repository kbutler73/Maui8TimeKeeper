using Maui8TimeKeeper.Models;
using Newtonsoft.Json;

namespace Maui8TimeKeeper;

public class TimeCardService
{
    public event EventHandler<TimeCard>? OnTimeCardAdded;

    private readonly Dictionary<Guid, TimeCard> _timeCards = [];

    public void AddTimeCard(TimeCard timeCard)
    {
        if (!_timeCards.ContainsKey(timeCard.Id))
        {
            _timeCards.Add(timeCard.Id, timeCard);
            OnTimeCardAdded?.Invoke(this, timeCard);
        }
    }

    public void RemoveTimeCard(Guid id)
    {
        if (_timeCards.ContainsKey(id))
        {
            _timeCards.Remove(id);
        }
    }

    public List<TimeCard> GetTimeCards()
    {
        return _timeCards.Values.ToList();
    }

    internal TimeCard GetTimeCard(Guid id)
    {
        if (_timeCards.ContainsKey(id))
        {
            return _timeCards[id];
        }

        return new TimeCard(string.Empty);
    }

    internal void UpdateTimeCard(TimeCard timeCard)
    {
        _timeCards[timeCard.Id] = timeCard;
    }

    internal Task Save()
    {
        var data = JsonConvert.SerializeObject(_timeCards);
        Preferences.Set("data", data);
        return Task.CompletedTask;
    }

    internal Task Load()
    {
        Dictionary<Guid, TimeCard> timecards;

        if (!Preferences.ContainsKey("data"))
        {
            timecards = new Dictionary<Guid, TimeCard>
            {
                { Guid.NewGuid(), new TimeCard("Meetings") { ChargeCode = "Pgm Req/FD" } },
                { Guid.NewGuid(), new TimeCard("Stories") { ChargeCode = "SW Requirements" } },
                { Guid.NewGuid(), new TimeCard("Defects") },
                { Guid.NewGuid(), new TimeCard("OH") { ChargeCode = "ForceX Software Engr" } },
                { Guid.NewGuid(), new TimeCard("General") },
                { Guid.NewGuid(), new TimeCard("Code Spike") }
            };
        }
        else
        {
            var data = Preferences.Get("data", string.Empty);
            timecards = JsonConvert.DeserializeObject<Dictionary<Guid, TimeCard>>(data) ?? [];
        }

        foreach (var timeCard in timecards.Values)
        {
            AddTimeCard(timeCard);
        }

        return Task.CompletedTask;
    }

    internal List<TimelineBoundary> GetTimelineBoundaries(DateTime localDate)
    {
        var dayStartLocal = localDate.Date;
        var dayEndLocal = dayStartLocal.AddDays(1);
        var timeline = new List<(TimeCard Card, Duration Duration, DateTime StartLocal, DateTime EndLocal)>();

        foreach (var card in _timeCards.Values)
        {
            foreach (var duration in card.Durations)
            {
                var startUtc = duration.StartTime;
                var endUtc = duration.EndTime == DateTime.MinValue ? DateTime.UtcNow : duration.EndTime;
                var startLocal = startUtc.ToLocalTime();
                var endLocal = endUtc.ToLocalTime();

                if (endLocal <= dayStartLocal || startLocal >= dayEndLocal)
                {
                    continue;
                }

                var clippedStartLocal = startLocal < dayStartLocal ? dayStartLocal : startLocal;
                var clippedEndLocal = endLocal > dayEndLocal ? dayEndLocal : endLocal;
                if (clippedEndLocal <= clippedStartLocal)
                {
                    continue;
                }

                timeline.Add((card, duration, clippedStartLocal, clippedEndLocal));
            }
        }

        timeline.Sort((a, b) => a.StartLocal.CompareTo(b.StartLocal));

        var result = new List<TimelineBoundary>();
        for (var i = 0; i < timeline.Count - 1; i++)
        {
            var left = timeline[i];
            var right = timeline[i + 1];
            if (left.Card.Id == right.Card.Id)
            {
                continue;
            }

            var leftMinutes = Math.Max(0, (int)Math.Floor((left.EndLocal - left.StartLocal).TotalMinutes));
            var rightMinutes = Math.Max(0, (int)Math.Floor((right.EndLocal - right.StartLocal).TotalMinutes));
            if (leftMinutes == 0 && rightMinutes == 0)
            {
                continue;
            }

            result.Add(new TimelineBoundary
            {
                LeftCard = left.Card,
                RightCard = right.Card,
                LeftDuration = left.Duration,
                RightDuration = right.Duration,
                LeftStartLocal = left.StartLocal,
                LeftEndLocal = left.EndLocal,
                RightStartLocal = right.StartLocal,
                RightEndLocal = right.EndLocal,
                MaxMinutesLeftToRight = leftMinutes,
                MaxMinutesRightToLeft = rightMinutes
            });
        }

        return result;
    }

    internal bool AdjustBoundary(TimelineBoundary boundary, int minutes)
    {
        if (minutes == 0)
        {
            return false;
        }

        var leftEndUtc = boundary.LeftDuration.EndTime == DateTime.MinValue
            ? DateTime.UtcNow
            : boundary.LeftDuration.EndTime;
        var rightEndUtc = boundary.RightDuration.EndTime == DateTime.MinValue
            ? DateTime.UtcNow
            : boundary.RightDuration.EndTime;
        var currentLeftMinutes = Math.Max(0, (int)Math.Floor((leftEndUtc - boundary.LeftDuration.StartTime).TotalMinutes));
        var currentRightMinutes = Math.Max(0, (int)Math.Floor((rightEndUtc - boundary.RightDuration.StartTime).TotalMinutes));

        var canMove = minutes < 0
            ? currentLeftMinutes >= Math.Abs(minutes)
            : currentRightMinutes >= minutes;

        if (!canMove)
        {
            return false;
        }

        boundary.LeftDuration.EndTime = boundary.LeftDuration.EndTime.AddMinutes(minutes);
        boundary.RightDuration.StartTime = boundary.RightDuration.StartTime.AddMinutes(minutes);
        boundary.LeftCard.UpdateTotalTime();
        boundary.RightCard.UpdateTotalTime();
        return true;
    }

    internal List<TimelineEditableSegment> GetEditableTimelineSegments(DateTime localDate)
    {
        var dayStartLocal = localDate.Date;
        var dayEndLocal = dayStartLocal.AddDays(1);
        var result = new List<TimelineEditableSegment>();

        foreach (var card in _timeCards.Values.OrderBy(x => x.Name))
        {
            foreach (var duration in card.Durations.OrderBy(x => x.StartTime))
            {
                var startUtc = duration.StartTime;
                var endUtc = duration.EndTime == DateTime.MinValue ? DateTime.UtcNow : duration.EndTime;
                var startLocal = startUtc.ToLocalTime();
                var endLocal = endUtc.ToLocalTime();
                if (endLocal <= dayStartLocal || startLocal >= dayEndLocal)
                {
                    continue;
                }

                var clippedStart = startLocal < dayStartLocal ? dayStartLocal : startLocal;
                var clippedEnd = endLocal > dayEndLocal ? dayEndLocal : endLocal;
                if (clippedEnd <= clippedStart)
                {
                    continue;
                }

                result.Add(new TimelineEditableSegment
                {
                    Card = card,
                    Duration = duration,
                    DayStartLocal = dayStartLocal,
                    DayEndLocal = dayEndLocal,
                    StartLocal = clippedStart,
                    EndLocal = clippedEnd
                });
            }
        }

        return result;
    }

    internal bool TryAdjustSegmentBoundary(TimelineEditableSegment segment, bool adjustStart, int deltaMinutes)
    {
        if (deltaMinutes == 0)
        {
            return true;
        }

        var delta = TimeSpan.FromMinutes(deltaMinutes);
        var newStart = segment.Duration.StartTime;
        var newEnd = segment.Duration.EndTime == DateTime.MinValue ? DateTime.UtcNow : segment.Duration.EndTime;
        if (adjustStart)
        {
            newStart = newStart.Add(delta);
        }
        else
        {
            newEnd = newEnd.Add(delta);
        }

        if (newEnd <= newStart || (newEnd - newStart).TotalMinutes < 1)
        {
            return false;
        }

        var boundaryUtc = adjustStart ? segment.Duration.StartTime : segment.Duration.EndTime;
        if (boundaryUtc == DateTime.MinValue)
        {
            boundaryUtc = DateTime.UtcNow;
        }

        Duration? linkedDuration = null;
        bool linkedIsStart = false;
        foreach (var card in _timeCards.Values)
        {
            foreach (var duration in card.Durations)
            {
                if (ReferenceEquals(duration, segment.Duration))
                {
                    continue;
                }

                if (adjustStart && duration.EndTime != DateTime.MinValue && duration.EndTime == boundaryUtc)
                {
                    linkedDuration = duration;
                    linkedIsStart = false;
                    break;
                }

                if (!adjustStart && duration.StartTime == boundaryUtc)
                {
                    linkedDuration = duration;
                    linkedIsStart = true;
                    break;
                }
            }

            if (linkedDuration != null)
            {
                break;
            }
        }

        if (linkedDuration != null)
        {
            var linkedStart = linkedDuration.StartTime;
            var linkedEnd = linkedDuration.EndTime == DateTime.MinValue ? DateTime.UtcNow : linkedDuration.EndTime;
            if (linkedIsStart)
            {
                linkedStart = linkedStart.Add(delta);
            }
            else
            {
                linkedEnd = linkedEnd.Add(delta);
            }

            if (linkedEnd <= linkedStart || (linkedEnd - linkedStart).TotalMinutes < 1)
            {
                return false;
            }
        }

        if (adjustStart)
        {
            segment.Duration.StartTime = newStart;
        }
        else
        {
            segment.Duration.EndTime = newEnd;
        }

        if (linkedDuration != null)
        {
            if (linkedIsStart)
            {
                linkedDuration.StartTime = linkedDuration.StartTime.Add(delta);
            }
            else
            {
                linkedDuration.EndTime = linkedDuration.EndTime.Add(delta);
            }
        }

        foreach (var card in _timeCards.Values)
        {
            card.UpdateTotalTime();
        }

        return true;
    }
}
