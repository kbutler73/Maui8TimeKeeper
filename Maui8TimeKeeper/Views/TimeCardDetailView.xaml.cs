using Maui8TimeKeeper.ViewModels;

namespace Maui8TimeKeeper.Views;

[QueryProperty(nameof(TimeCardId), "Id")]
public partial class TimeCardDetailView : ContentPage
{
    private readonly TimeCardDetailViewModel _viewModel;

    public TimeCardDetailView(TimeCardDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    public string TimeCardId { get; set; } = "";

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!string.IsNullOrWhiteSpace(TimeCardId) && Guid.TryParse(TimeCardId, out var id))
        {
            await _viewModel.LoadTimeCard(id);
        }
    }
}