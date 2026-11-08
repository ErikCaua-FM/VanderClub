using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmEventos : ClubPage
{
    private List<Evento> _eventos = [];
    public frmEventos(AppState state) : base(state, "Eventos")
    {
        InitializeComponent();
        FiltroStatus.ItemsSource = new[] { "Todos", "Agendado", "Confirmado", "Cancelado", "Realizado" };
        FiltroStatus.SelectedIndex = 0;
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Load); }
    private async Task Load()
    {
        UnidadeLabel.Text = State.Atual?.Unidade.Nome;
        _eventos = await State.Club.EventosAsync(); Render();
    }
    private async void Atualizar(object? sender, EventArgs e)
        => await Run(async () => { await State.Club.LimparCacheAsync(); await Load(); }, sender as Button);
    private void Filtrar(object? sender, EventArgs e) { if (Lista is not null) Render(); }
    private void Render()
    {
        Lista.Clear(); var query = (Pesquisa.Text ?? "").Trim(); var status = FiltroStatus.SelectedItem?.ToString() ?? "Todos";
        var list = _eventos.Where(e => status == "Todos" || e.Status == status)
            .Where(e => (e.Nome + " " + e.Local).Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(e => e.FimUtc < DateTime.UtcNow).ThenBy(e => e.InicioUtc).ToList();
        if (list.Count == 0) Lista.Add(Ui.Empty("Nenhum evento encontrado", "A programação é publicada pela administração. Tente outro filtro ou atualize a lista."));
        foreach (var e in list)
        {
            var image = new[] { "festa.png", "social.png", "futebol.png", "quiosque.png" }.Contains(e.Imagem) ? e.Imagem : "festa.png";
            var card = Ui.Card(Ui.Stack(new Image { Source = image, Aspect = Aspect.AspectFill, HeightRequest = 165 },
                Ui.Badge(e.Status, e.Status == "Cancelado" ? Color.FromArgb("#AD3449") : Ui.Teal),
                Ui.Text(e.Nome, 22, Ui.Ink, true), Ui.Text(Horario.DataHora(e.InicioUtc), 14, Ui.Muted),
                Ui.Text(e.Local, 14, Ui.Muted), Ui.Text("Ver informações  ›", 15, Ui.Teal, true)));
            Ui.Tap(card, async () => await Run(() => Go(new frmDetalhesEvento(State, e.Id)))); Lista.Add(card);
        }
    }
}
