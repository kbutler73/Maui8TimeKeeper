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
}
