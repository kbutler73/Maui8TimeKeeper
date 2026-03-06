using CommunityToolkit.Mvvm.ComponentModel;
using Maui8TimeKeeper.Models;

namespace Maui8TimeKeeper.ViewModels;

public partial class TimelineBoundaryViewModel : ObservableObject
{
    public TimelineBoundaryViewModel(TimelineBoundary boundary)
    {
        Boundary = boundary;
        MoveLeftToRight5 = new BoundaryAdjustmentAction { Boundary = this, Minutes = -5 };
        MoveLeftToRight10 = new BoundaryAdjustmentAction { Boundary = this, Minutes = -10 };
        MoveRightToLeft5 = new BoundaryAdjustmentAction { Boundary = this, Minutes = 5 };
        MoveRightToLeft10 = new BoundaryAdjustmentAction { Boundary = this, Minutes = 10 };
    }

    public TimelineBoundary Boundary { get; }

    public string LeftName => Boundary.LeftCard.Name;

    public string RightName => Boundary.RightCard.Name;

    public string LeftCode => Boundary.LeftCard.ChargeCode;

    public string RightCode => Boundary.RightCard.ChargeCode;

    public DateTime BoundaryTimeLocal => Boundary.RightStartLocal;

    public string TimeRangeLabel =>
        $"{Boundary.LeftStartLocal:HH:mm}-{Boundary.LeftEndLocal:HH:mm}  |  {Boundary.RightStartLocal:HH:mm}-{Boundary.RightEndLocal:HH:mm}";

    public int LeftMinutes => Boundary.LeftMinutes;

    public int RightMinutes => Boundary.RightMinutes;

    public string LeftDurationLabel => $"{LeftMinutes}m";

    public string RightDurationLabel => $"{RightMinutes}m";

    public string LeftColor => ColorFor(Boundary.LeftCard.Id);

    public string RightColor => ColorFor(Boundary.RightCard.Id);

    public int MaxLeftToRight => Boundary.MaxMinutesLeftToRight;

    public int MaxRightToLeft => Boundary.MaxMinutesRightToLeft;

    public bool CanMoveLeftToRight5 => MaxLeftToRight >= 5;

    public bool CanMoveLeftToRight10 => MaxLeftToRight >= 10;

    public bool CanMoveRightToLeft5 => MaxRightToLeft >= 5;

    public bool CanMoveRightToLeft10 => MaxRightToLeft >= 10;

    public BoundaryAdjustmentAction MoveLeftToRight5 { get; }

    public BoundaryAdjustmentAction MoveLeftToRight10 { get; }

    public BoundaryAdjustmentAction MoveRightToLeft5 { get; }

    public BoundaryAdjustmentAction MoveRightToLeft10 { get; }

    public double LeftVisualWidth
    {
        get
        {
            var total = Math.Max(1, LeftMinutes + RightMinutes);
            return 240.0 * LeftMinutes / total;
        }
    }

    public double RightVisualWidth
    {
        get
        {
            var total = Math.Max(1, LeftMinutes + RightMinutes);
            return 240.0 * RightMinutes / total;
        }
    }

    private static string ColorFor(Guid id)
    {
        var hash = Math.Abs(id.GetHashCode());
        var r = 80 + (hash & 0x3F);
        var g = 80 + ((hash >> 6) & 0x3F);
        var b = 80 + ((hash >> 12) & 0x3F);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
