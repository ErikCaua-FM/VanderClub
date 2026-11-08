using VanderClub.Helpers;
using VanderClub.Models;
using Microsoft.Maui.Controls.Shapes;
namespace VanderClub.Views;
public sealed partial class frmInicio : ClubPage
{
    public frmInicio(AppState state) : base(state, "VanderClub") { InitializeComponent(); Body = LayoutPrincipal; Shell.SetNavBarIsVisible(this, false); }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Load); }
    private async Task Load()
    {
        await State.AtualizarAsync(); var c = State.Atual!;
        var menu = new Button { Text = "☰", TextColor = Colors.White, BackgroundColor = Colors.Transparent, FontSize = 25, WidthRequest = 48, HeightRequest = 48 };
        menu.Clicked += async (_, _) => await Run(Menu);
        var exit = new Button { Text = "Sair", TextColor = Colors.White, BackgroundColor = Colors.Transparent, FontSize = 15 };
        exit.Clicked += async (_, _) => await Run(Logout);
        var top = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Padding = new Thickness(12, 20, 14, 0) };
        top.Add(menu, 0, 0); top.Add(new Label { Text = "VanderClub", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(12, 0) }, 1, 0); top.Add(exit, 2, 0);
        var header = new Grid { HeightRequest = 160, BackgroundColor = Ui.Purple };
        header.Add(new Image { Source = "cabecalho.png", Aspect = Aspect.AspectFill }); header.Add(top);
        top.VerticalOptions = LayoutOptions.Start;
        var avatar = Ui.Card(new Label { Text = c.Perfil.Iniciais, TextColor = Colors.White, FontSize = 25, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }, 0, Ui.Purple, 40);
        avatar.WidthRequest = 70; avatar.HeightRequest = 70;
        var profile = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 15 };
        profile.Add(avatar);
        var unidades = await State.Club.UnidadesAsync();
        var allowed = c.Associacao.TodaRede ? unidades : unidades.Where(u => u.Id == c.Associacao.UnidadeOrigemId).ToList();
        var select = new Picker { ItemsSource = allowed, Title = "Unidade selecionada", TextColor = Ui.Teal, FontSize = 14, HorizontalOptions = LayoutOptions.Fill };
        select.SelectedItem = allowed.First(u => u.Id == c.Unidade.Id);
        select.SelectedIndexChanged += async (_, _) =>
        {
            if (select.SelectedItem is Unidade u && u.Id != State.Atual?.Unidade.Id)
                await Run(async () => { await State.Club.MudarUnidadeAsync(u.Id); await Load(); });
        };
        profile.Add(Ui.Stack(Ui.Text(c.Perfil.Nome, 21, Ui.Ink, true), Ui.Text(c.Perfil.Titular ? "Sócio titular / fundador" : "Sócio convidado", 14, Ui.Muted), select), 1, 0);
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 12, RowSpacing = 14 };
        for (int i = 0; i < 5; i++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var tiles = new (string Title, string Icon, string Image, Func<Page> Page, bool Owner)[] {
            ("Sócios-convidados", "◉", "pessoas.png", () => new frmSociosConvidados(State), true),
            ("Visitantes", "⌂", "visitantes.png", () => new frmVisitantes(State), true),
            ("Solicitações", "✎", "solicitacoes.png", () => new frmChamados(State, "Solicitação"), false),
            ("Reservas", "▦", "reservas.png", () => new frmReservas(State), false),
            ("Reclamações", "!", "reclamacoes.png", () => new frmChamados(State, "Reclamação"), false),
            ("Chat", "☏", "chat.png", () => new frmChat(State), false),
            ("Votações", "✓", "votacoes.png", () => new frmVotacoes(State), false),
            ("Mensalidades", "$", "mensalidades.png", () => new frmMensalidades(State), true),
            ("Eventos", "▦", "festa.png", () => new frmEventos(State), false),
            ("Minha Conta", "◉", "pessoas.png", () => new frmMinhaConta(State), false)
        };
        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i];
            var card = Feature(tile.Title, tile.Icon, tile.Image);
            Ui.Tap(card, async () => await Run(async () =>
            {
                if (tile.Owner && !c.Perfil.Titular) { await DisplayAlertAsync("Área do titular", "Esta função é gerenciada pelo titular do plano.", "Entendi"); return; }
                await Go(tile.Page());
            }));
            grid.Add(card, i % 2, i / 2);
        }
        var contents = Ui.Stack(profile, Ui.Rule(), Ui.Text("Funcionalidades principais", 22, Ui.Muted), grid);
        var main = Ui.Card(contents, 20, radius: 28); main.Margin = new Thickness(16, -55, 16, 0);
        var footer = Ui.Stack(Ui.Badge(c.Associacao.PlanoNome, Ui.Purple), Ui.Text($"Vigência até {c.Associacao.FimLocal.AddDays(-1):dd/MM/yyyy} • {c.Unidade.Cidade}", 12, Ui.Muted),
            Ui.Text("Seu clube, sempre por perto.", 12, Ui.Muted)); footer.Margin = new Thickness(26, 0, 26, 26);
        Content = new ScrollView { Content = new VerticalStackLayout { Spacing = 22, Children = { header, main, footer } } };
    }
    private static View Feature(string title, string icon, string image)
    {
        var layout = new Grid { HeightRequest = 172 };
        layout.Add(new Image { Source = image, Aspect = Aspect.AspectFill });
        layout.Add(new BoxView { Background = new LinearGradientBrush(new GradientStopCollection {
            new GradientStop(Color.FromArgb("#00303C50"), 0), new GradientStop(Color.FromArgb("#CC303C50"), 1) }, new Point(0, 0), new Point(0, 1)) });
        var circle = Ui.Card(new Label { Text = icon, FontSize = 22, TextColor = Ui.Purple, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }, 0, Color.FromArgb("#EBFFFFFF"), 22);
        circle.WidthRequest = 40; circle.HeightRequest = 40; circle.HorizontalOptions = LayoutOptions.Start; circle.VerticalOptions = LayoutOptions.Start; circle.Margin = 12;
        layout.Add(circle);
        var text = Ui.Text(title, 18, Colors.White, true); text.VerticalOptions = LayoutOptions.End; text.Margin = new Thickness(12, 0, 10, 14); layout.Add(text);
        return new Border { Content = layout, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 14 } };
    }
    private async Task Menu()
    {
        string choice = await DisplayActionSheetAsync("VanderClub", "Fechar", null, "Minha Conta", "Eventos", "Sair da conta");
        if (choice == "Minha Conta") await Go(new frmMinhaConta(State));
        else if (choice == "Eventos") await Go(new frmEventos(State));
        else if (choice == "Sair da conta") await Logout();
    }
    private async Task Logout()
    {
        if (!await DisplayAlertAsync("Sair da conta?", "Sua sessão neste dispositivo será encerrada e o cache será limpo.", "Sair", "Continuar")) return;
        await State.SairAsync(); App.Root(new frmLogin(State));
    }
}
