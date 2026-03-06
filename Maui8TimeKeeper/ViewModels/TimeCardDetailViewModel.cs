using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui8TimeKeeper.Models;

namespace Maui8TimeKeeper.ViewModels;

public partial class TimeCardDetailViewModel : ObservableObject
{
    private readonly TimeCardService _timeCardService;

    [ObservableProperty]
    private TimeCard? timeCard;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string chargeCode = string.Empty;

    [ObservableProperty]
    private string note = string.Empty;

    public TimeCardDetailViewModel(TimeCardService timeCardService)
    {
        _timeCardService = timeCardService;
    }

    public async Task LoadTimeCard(Guid id)
    {
        await Task.Run(() =>
        {
            TimeCard = _timeCardService.GetTimeCard(id);
            ChargeCode = TimeCard.ChargeCode;
            Name = TimeCard.Name;
            Note = TimeCard.Notes;
        });
    }

    [RelayCommand]
    public async Task Save()
    {
        if (TimeCard != null)
        {
            TimeCard.Name = Name;
            TimeCard.Notes = Note;
            TimeCard.ChargeCode = ChargeCode;
            _timeCardService.UpdateTimeCard(TimeCard);
        }

        await _timeCardService.Save();
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public void Clear()
    {
        Note = string.Empty;
    }
}
