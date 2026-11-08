using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Helpers;
public abstract class ClubPage : ContentPage
{
    protected AppState State { get; }
    protected bool Running { get; private set; }
    protected VerticalStackLayout Body { get; set; } = new() { Spacing = 18, Padding = new Thickness(20, 20, 20, 32), MaximumWidthRequest = 680, HorizontalOptions = LayoutOptions.Fill };
    protected ClubPage(AppState state, string title)
    {
        State = state; Title = title; BackgroundColor = Ui.Surface;
        Content = new ScrollView { Content = Body };
    }
    protected async Task Run(Func<Task> action, Button? button = null)
    {
        if (Running) return;
        Running = true;
        if (button is not null) button.IsEnabled = false;
        try { await action(); }
        catch (ApiException ex) when (ex.StatusCode == 401) { await State.EncerrarLocalAsync(); App.Root(new VanderClub.Views.frmLogin(State)); await DisplayAlertAsync("Sessão encerrada", "Entre novamente para continuar.", "OK"); }
        catch (RegraException ex) { await DisplayAlertAsync("Confira os dados", ex.Message, "Entendi"); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("Não foi possível concluir", "Não conseguimos concluir a operação. Tente novamente. Nenhuma confirmação foi recebida do servidor.", "OK");
        }
        finally { Running = false; if (button is not null) button.IsEnabled = true; }
    }
    protected Button Action(string label, Func<Task> action, bool secondary = false)
    {
        var b = Ui.Button(label, secondary); b.Clicked += async (_, _) => await Run(action, b); return b;
    }
    protected Task Go(Page page) => Navigation.PushAsync(page);
    protected void Heading(string title, string subtitle) { Body.Add(Ui.Text(title, 26, Ui.Ink, true)); Body.Add(Ui.Text(subtitle, 14, Ui.Muted)); }
    protected async Task Saved(string message)
    {
        await DisplayAlertAsync("Tudo certo", message, "Continuar"); await Navigation.PopAsync();
    }
}
