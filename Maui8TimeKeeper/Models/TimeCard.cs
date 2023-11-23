using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Maui8TimeKeeper.Models;

public partial class TimeCard : ObservableObject
{
    public TimeCard(string name)
    {
        Id = Guid.NewGuid();
        Name = name;

        Task.Run(UpdateLoop);
    }

    private async Task UpdateLoop()
    {
        while (true)
        {
            if (IsActive)
            {
                UpdateTotalTime();
            }
            await Task.Delay(500);
        }
    }

    public Guid Id { get; }

    public ObservableCollection<Duration> Durations { get; } = [];

    public List<string> Notes { get; } = [];

    public string Name { get; }

    [ObservableProperty]
    private bool isActive;

    public void ToggleIsActive()
    {
        IsActive = !IsActive;
        if (IsActive)
        {
            var duration = new Duration
            {
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.MinValue
            };
            Durations.Add(duration);
        }
        else
        {
            var lastDuration = Durations.FirstOrDefault(x => x.EndTime == DateTime.MinValue);
            if (lastDuration != null)
            {
                lastDuration.EndTime = DateTime.UtcNow;
            }
        }
    }

    [ObservableProperty]
    private TimeSpan totalTime;

    public void UpdateTotalTime()
    {
        var total = TimeSpan.Zero;
        foreach (var d in Durations)
        {
            var endTime = d.EndTime == DateTime.MinValue ? DateTime.UtcNow : d.EndTime;
            total += endTime - d.StartTime;
        }

        TotalTime = total;
        DecimalTime = Math.Round(total.TotalHours, 1, MidpointRounding.ToPositiveInfinity);

        foreach (var d in Durations)
        {
            d.Update();
        }
    }

    [ObservableProperty]
    private double decimalTime;
}