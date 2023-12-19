using Maui8TimeKeeper.Models;
using Newtonsoft.Json;

namespace Maui8TimeKeeper;

public class TimeCardService
{
    public event EventHandler<TimeCard>? OnTimeCardAdded;

    private Dictionary<Guid, TimeCard> _timeCards = new();

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

        return new TimeCard("");
    }

    internal void UpdateTimeCard(TimeCard timeCard)
    {
        _timeCards[timeCard.Id] = timeCard;
    }

    internal async Task Save()
    {
        await Task.Run(() =>
        {
            var data = JsonConvert.SerializeObject(_timeCards);
            Preferences.Set("data", data);
        });
    }

    internal async Task Load()
    {
        await Task.Run(() =>
        {
            var data = Preferences.Get("data", "");
            var timecards = JsonConvert.DeserializeObject<Dictionary<Guid, TimeCard>>(data);

            if (timecards is null) return;

            foreach (var timeCard in timecards.Values)
            {
                AddTimeCard(timeCard);
            }
        });
    }
}