using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui8TimeKeeper.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Maui8TimeKeeper.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public MainPageViewModel()
    {
        TimeCards = [];
        TimeCards.CollectionChanged += TimeCards_CollectionChanged;

        Load();

        //Task.Run(UpdateLoop);
    }

    private void Load()
    {
        if (Preferences.ContainsKey("data"))
        {
            var data = Preferences.Get("data", "");
            var cards = JsonConvert.DeserializeObject<List<TimeCard>>(data);

            if (cards is null) return;

            foreach (var item in cards)
            {
                TimeCards.Add(item);
            }
        }
    }

    private void Save()
    {
        var data = JsonConvert.SerializeObject(TimeCards);
        Preferences.Set("data", data);
    }

    private void TimeCards_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var card in TimeCards)
        {
            card.PropertyChanged -= Card_PropertyChanged;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                card.PropertyChanged += Card_PropertyChanged;
            }
        }
    }

    private void Card_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimeCard.TotalTime))
        {
            var time = TimeSpan.Zero;
            var dTime = 0.0;
            foreach (var card in TimeCards)
            {
                time += card.TotalTime;
                dTime += card.DecimalTime;
            }

            TotalTime = time;
            DecimalTime = dTime;
            TimeLeft = TimeSpan.FromHours(LengthOfDay) - TotalTime;
            EndOfDay = DateTime.Now + TimeLeft;
        }
    }

    //private async Task UpdateLoop()
    //{
    //    while (true)
    //    {
    //        await Task.Delay(100);
    //    }
    //}

    private ObservableCollection<TimeCard> _timeCards = [];

    public ObservableCollection<TimeCard> TimeCards
    {
        get => _timeCards;
        set
        {
            _timeCards = value;
        }
    }

    [ObservableProperty]
    private TimeSpan totalTime;

    [ObservableProperty]
    private double decimalTime;

    [ObservableProperty]
    private DateTime endOfDay;

    [ObservableProperty]
    private TimeSpan timeLeft;

    [ObservableProperty]
    private double lengthOfDay = 9.0;

    [ObservableProperty]
    private bool showDetails = true;

    [RelayCommand]
    private void AddTimeCard(string name)
    {
        var timeCard = new TimeCard(name);
        TimeCards.Add(timeCard);
        timeCard.Notes.Add("test note");
        Save();
    }

    [RelayCommand]
    private void ToggleEnabled(TimeCard timeCard)
    {
        var enabledTimeCard = TimeCards.FirstOrDefault(x => x.IsActive);
        if (enabledTimeCard != null)
        {
            enabledTimeCard.ToggleIsActive();
        }

        if (enabledTimeCard?.Id != timeCard.Id)
        {
            timeCard.ToggleIsActive();
        }
        Save();
    }
}