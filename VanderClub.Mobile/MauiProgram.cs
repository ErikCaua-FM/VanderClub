using VanderClub.Helpers;
namespace VanderClub;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddSingleton<AppState>();
        return builder.Build();
    }
}
