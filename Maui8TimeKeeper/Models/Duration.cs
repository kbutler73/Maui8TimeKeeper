using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Maui8TimeKeeper.Models;

public partial class Duration : ObservableObject
{
    [ObservableProperty]
    private DateTime startTime;

    [ObservableProperty]
    private DateTime endTime;

    [ObservableProperty]
    private TimeSpan elapsed;

    [ObservableProperty]
    private double decimalElapsed;

    public void Update()
    {
        var end = EndTime == DateTime.MinValue ? DateTime.UtcNow : EndTime;
        Elapsed = end - StartTime;
        DecimalElapsed = Math.Round(Elapsed.TotalHours, 1, MidpointRounding.ToPositiveInfinity);
    }
}