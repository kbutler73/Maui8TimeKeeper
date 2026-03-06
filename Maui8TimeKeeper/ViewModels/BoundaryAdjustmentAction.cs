namespace Maui8TimeKeeper.ViewModels;

public sealed class BoundaryAdjustmentAction
{
    public required TimelineBoundaryViewModel Boundary { get; init; }

    // Negative values move time from left code to right code.
    // Positive values move time from right code to left code.
    public required int Minutes { get; init; }
}
