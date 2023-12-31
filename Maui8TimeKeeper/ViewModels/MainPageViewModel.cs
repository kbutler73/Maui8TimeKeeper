﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui8TimeKeeper.Models;
using Maui8TimeKeeper.Views;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Maui8TimeKeeper.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly Timer _timer = new Timer(10000);
    private readonly TimeCardService _timeCardService;

    private const string locked = "\ue897";
    private const string lock_open = "\ue898";

    public MainPageViewModel(TimeCardService timeCardService)
    {
        _timer.AutoReset = false;
        _timer.Elapsed += _timer_Elapsed;

        TimeCards = [];
        TimeCards.CollectionChanged += TimeCards_CollectionChanged;
        _timeCardService = timeCardService;
        _timeCardService.OnTimeCardAdded += _timeCardService_OnTimeCardAdded;
        Task.Run(Load);
    }

    private void _timeCardService_OnTimeCardAdded(object? sender, TimeCard timeCard)
    {
        TimeCards.Add(timeCard);
    }

    private async Task Load()
    {
        try
        {
            LengthOfDay = Preferences.Get(nameof(LengthOfDay), 9.0);

            EmailAddress = Preferences.Get("email", "Set Email");

            await _timeCardService.Load();
        }
        catch (Exception ex)
        {
        }
    }

    private void Save()
    {
        _timeCardService?.Save();

        Preferences.Set(nameof(LengthOfDay), LengthOfDay);
    }

    private void TimeCards_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (TimeCard card in e.NewItems)
            {
                card.PropertyChanged += Card_PropertyChanged;
            }
        }
        Save();
    }

    private void Card_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e == null || e.PropertyName == nameof(TimeCard.TotalTime))
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
    private string emailAddress = "Set Email Address";

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
    private bool showDetails = false;

    [ObservableProperty]
    private string entryText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditingNotEnabled))]
    private bool editingEnabled = false;

    public bool EditingNotEnabled => !EditingEnabled;

    [ObservableProperty]
    private string editingEnabledGlyph = locked;

    [RelayCommand]
    private void AddTimeCard(string name)
    {
        var timeCard = new TimeCard(name);
        _timeCardService.AddTimeCard(timeCard);
        EntryText = "";
    }

    [RelayCommand]
    private void ToggleEnabled(TimeCard timeCard)
    {
        if (!EditingEnabled) return;

        _timer.Stop();
        _timer.Start();

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

    [RelayCommand]
    private void DeleteCard(TimeCard timeCard)
    {
        timeCard.PropertyChanged -= Card_PropertyChanged;
        _timeCardService.RemoveTimeCard(timeCard.Id);
        TimeCards.Remove(timeCard);
    }

    [RelayCommand]
    private void ClearCards() //TODO: move this to service?
    {
        foreach (var timeCard in TimeCards)
        {
            timeCard.IsActive = false;
            timeCard.Durations.Clear();
            timeCard.UpdateTotalTime();
        }
        Card_PropertyChanged(this, null);
        Save();
    }

    [RelayCommand]
    private async Task SendEmail()
    {
        if (Email.Default.IsComposeSupported)
        {
            string subject = $"Time for {DateTime.Now.ToShortDateString()}";

            string body = "";
            foreach (var timeCard in TimeCards)
            {
                body += $"{timeCard.Name} ({timeCard.ChargeCode}):    {timeCard.DecimalTime}{Environment.NewLine}{timeCard.Notes}{Environment.NewLine}{Environment.NewLine}";
            }
            string[] recipients = [EmailAddress];

            var message = new EmailMessage
            {
                Subject = subject,
                Body = body,
                BodyFormat = EmailBodyFormat.PlainText,
                To = new List<string>(recipients)
            };

            await Email.Default.ComposeAsync(message);
        }
    }

    [RelayCommand]
    private void SetEmail(string email)
    {
        if (!string.IsNullOrEmpty(email))
        {
            EmailAddress = email;
            Preferences.Set("email", email);
        }
    }

    [RelayCommand]
    private void SetDayLength(string value)
    {
        if (double.TryParse(value, out var len))
        {
            LengthOfDay = len;
        }
        EntryText = "";
        Save();
    }

    [RelayCommand]
    private void ToggleShowDetails()
    {
        ShowDetails = !ShowDetails;
    }

    [RelayCommand]
    private async Task EditCard(TimeCard timeCard)
    {
        await Shell.Current.GoToAsync($"{nameof(TimeCardDetailView)}?Id={timeCard.Id}");
    }

    [RelayCommand]
    private void ToggleEditingEnabled()
    {
        if (EditingEnabled)
        {
            _timer.Stop();
            EditingEnabled = false;
            EditingEnabledGlyph = locked;
        }
        else
        {
            EditingEnabled = true;
            EditingEnabledGlyph = lock_open;
            _timer.Start();
        }
    }

    private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        EditingEnabled = false;
        EditingEnabledGlyph = locked;
    }
}