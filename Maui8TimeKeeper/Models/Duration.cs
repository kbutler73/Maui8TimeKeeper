using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Maui8TimeKeeper.Models;

public partial class Duration : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocalStartTime))]
    private DateTime startTime;

    public DateTime LocalStartTime => StartTime.ToLocalTime();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocalEndTime))]
    private DateTime endTime;

    public DateTime LocalEndTime => EndTime.ToLocalTime();

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