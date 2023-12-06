using Maui8TimeKeeper.ViewModels;
using Maui8TimeKeeper.Views;
using Microsoft.Extensions.Logging;

namespace Maui8TimeKeeper;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services
            .AddSingleton<MainPageViewModel>()
            .AddSingleton<MainPage>()
            .AddTransient<TimeCardDetailView>()
            .AddTransient<TimeCardDetailViewModel>()
            .AddSingleton<TimeCardService>()
            ;

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}