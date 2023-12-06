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
    private string name;

    public TimeCardDetailViewModel(TimeCardService timeCardService)
    {
        _timeCardService = timeCardService;
    }

    public async Task LoadTimeCard(Guid id)
    {
        await Task.Run(() =>
        {
            TimeCard = _timeCardService.GetTimeCard(id);
            Name = TimeCard.Name;
        });
    }

    [RelayCommand]
    public async Task Save()
    {
        if (TimeCard != null)
        {
            TimeCard.Name = Name;
            _timeCardService.UpdateTimeCard(TimeCard);
        }
        await Shell.Current.GoToAsync("..");
    }
}