using VanderClub.Helpers;
namespace VanderClub.Views;
public sealed partial class frmLogin : ClubPage
{
    private readonly Entry _email = Ui.Entry("seuemail@exemplo.com", Keyboard.Email);
    private readonly Entry _senha = Ui.Entry("Sua senha", secret: true);
    public frmLogin(AppState state, string? email = null) : base(state, "Entrar")
    {
        InitializeComponent(); Body = LayoutPrincipal;
        Shell.SetNavBarIsVisible(this, false); _email.Text = email;
        var hero = new Grid { HeightRequest = 230, BackgroundColor = Ui.Purple };
        hero.Add(new Image { Source = "cabecalho.png", Aspect = Aspect.AspectFill });
        hero.Add(new VerticalStackLayout { Padding = new Thickness(28, 60, 28, 24), Spacing = 10,
            Children = { Ui.Text("VanderClub", 37, Colors.White, true), Ui.Text("Uma rede de bons momentos.", 17, Colors.White) } });
        var login = Action("Login", Entrar);
        var link = Ui.Text("Ainda não é sócio? Escolha seu plano", 15, Ui.Teal, true);
        link.HorizontalTextAlignment = TextAlignment.Center; link.Margin = new Thickness(0, 12);
        Ui.Tap(link, async () => await Run(() => Go(new frmPlanos(State))));
        var show = new CheckBox { Color = Ui.Teal }; show.CheckedChanged += (_, e) => _senha.IsPassword = !e.Value;
        var showRow = new HorizontalStackLayout { Spacing = 2, Children = { show, new Label { Text = "Mostrar senha", VerticalOptions = LayoutOptions.Center, FontSize = 13, TextColor = Ui.Muted } } };
        var card = Ui.Card(Ui.Stack(Ui.Text("Que bom ter você aqui", 25, Ui.Ink, true), Ui.Text("Entre na sua conta de sócio.", 14, Ui.Muted),
            Ui.Field("E-mail", _email), Ui.Field("Senha", _senha), showRow, login, link), 24, radius: 28);
        card.Margin = new Thickness(20, -25, 20, 0);
        var content = new VerticalStackLayout { Spacing = 20, Children = { hero, card } };
        Content = new ScrollView { Content = content };
        _senha.Completed += async (_, _) => await Run(Entrar, login);
    }
    private async Task Entrar()
    {
        var remembered = await State.EntrarAsync(_email.Text ?? "", _senha.Text ?? "");
        _senha.Text = "";
        if (!remembered) await DisplayAlertAsync("Sessão iniciada", "Este dispositivo não permitiu salvar a sessão com segurança. Você poderá usar o aplicativo e fazer login novamente ao reabri-lo.", "Continuar");
        App.Root(new frmInicio(State));
    }
}
