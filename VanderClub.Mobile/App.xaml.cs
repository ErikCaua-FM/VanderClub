using VanderClub.Helpers;
using VanderClub.Views;
using System.Globalization;
namespace VanderClub;
public partial class App : Application
{
    private readonly AppState _state;
    public App(AppState state)
    {
        InitializeComponent();
        _state = state;
        UserAppTheme = AppTheme.Light;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell(new MainPage(_state))) { Title = "VanderClub" };
#if WINDOWS
        window.Width = 480; window.Height = 850; window.MinimumWidth = 380; window.MinimumHeight = 650;
#endif
        return window;
    }
    public static void Root(Page page)
    {
        if (Current?.Windows.FirstOrDefault() is { } window) window.Page = new AppShell(page);
    }
}
