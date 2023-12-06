using Maui8TimeKeeper.Models;

namespace Maui8TimeKeeper;

public class TimeCardService
{
    private readonly Dictionary<Guid, TimeCard> _timeCards = new();

    public void AddTimeCard(TimeCard timeCard)
    {
        if (!_timeCards.ContainsKey(timeCard.Id))
        {
            _timeCards.Add(timeCard.Id, timeCard);
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
}