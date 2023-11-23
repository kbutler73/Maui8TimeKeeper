using Maui8TimeKeeper.ViewModels;

namespace Maui8TimeKeeper;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}