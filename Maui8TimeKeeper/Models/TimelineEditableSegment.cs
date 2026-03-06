namespace Maui8TimeKeeper.Models;

public sealed class TimelineEditableSegment
{
    public required TimeCard Card { get; init; }

    public required Duration Duration { get; init; }

    public required DateTime DayStartLocal { get; init; }

    public required DateTime DayEndLocal { get; init; }

    public required DateTime StartLocal { get; init; }

    public required DateTime EndLocal { get; init; }
}
