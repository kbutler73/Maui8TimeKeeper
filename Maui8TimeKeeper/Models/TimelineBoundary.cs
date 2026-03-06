namespace Maui8TimeKeeper.Models;

public sealed class TimelineBoundary
{
    public required TimeCard LeftCard { get; init; }

    public required TimeCard RightCard { get; init; }

    public required Duration LeftDuration { get; init; }

    public required Duration RightDuration { get; init; }

    public required DateTime LeftStartLocal { get; init; }

    public required DateTime LeftEndLocal { get; init; }

    public required DateTime RightStartLocal { get; init; }

    public required DateTime RightEndLocal { get; init; }

    public required int MaxMinutesLeftToRight { get; init; }

    public required int MaxMinutesRightToLeft { get; init; }

    public int LeftMinutes => Math.Max(0, (int)Math.Round((LeftEndLocal - LeftStartLocal).TotalMinutes));

    public int RightMinutes => Math.Max(0, (int)Math.Round((RightEndLocal - RightStartLocal).TotalMinutes));
}
