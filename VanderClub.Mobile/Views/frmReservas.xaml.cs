using VanderClub.Helpers;
using VanderClub.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Globalization;
namespace VanderClub.Views;
public sealed partial class frmReservas : ClubPage
{
    private readonly VerticalStackLayout _list = new() { Spacing = 22 };
    private readonly SearchBar _search = new() { Placeholder = "Busque por recursos aqui", TextColor = Ui.Ink, PlaceholderColor = Ui.Muted, BackgroundColor = Colors.Transparent };
    private readonly Button _resources, _mine;
    private List<Area> _areas = [];
    private bool _showMine;
    public frmReservas(AppState state) : base(state, "Recursos para reserva")
    {
        InitializeComponent(); Body = LayoutPrincipal;
        _resources = Action("Recursos", async () => { _showMine = false; await Load(); });
        _mine = Action("Minhas reservas", async () => { _showMine = true; await Load(); }, true);
        Body.Add(Ui.Text("Unidade: " + (state.Atual?.Unidade.Nome ?? ""), 14, Ui.Muted));
        Body.Add(Ui.Row(_resources, _mine)); Body.Add(Ui.Card(_search, 0, radius: 26)); Body.Add(_list);
        _search.TextChanged += (_, _) => { if (!_showMine) DrawAreas(); };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Load); }
    private async Task Load()
    {
        _resources.BackgroundColor = _showMine ? Ui.Pale : Ui.Teal; _resources.TextColor = _showMine ? Ui.Purple : Colors.White;
        _mine.BackgroundColor = _showMine ? Ui.Teal : Ui.Pale; _mine.TextColor = _showMine ? Colors.White : Ui.Purple;
        _search.IsVisible = !_showMine;
        if (_showMine)
        {
            _list.Clear(); var list = await State.Club.MinhasReservasAsync();
            if (list.Count == 0) _list.Add(Ui.Empty("Sua agenda está livre", "Escolha um recurso e faça a primeira reserva."));
            foreach (var item in list)
            {
                var r = item.Reserva;
                var card = Ui.Stack(Ui.Text(item.Area.Nome, 21, Ui.Ink, true), Ui.Text(r.Protocolo, 12, Ui.Teal, true),
                    Ui.Text($"Entrada: {Horario.DataHora(r.InicioUtc)}\nSaída: {Horario.DataHora(r.FimUtc)}", 15),
                    Ui.Badge(r.Status == "Confirmada" && r.FimUtc < State.Clock.UtcNow ? "Concluída" : r.Status, Ui.StatusColor(r.Status)));
                if (r.Status == "Confirmada" && r.InicioUtc > State.Clock.UtcNow)
                    card.Add(Action("Cancelar reserva", async () =>
                    {
                        if (await DisplayAlertAsync("Cancelar reserva?", "O horário será liberado para outros associados.", "Cancelar reserva", "Voltar"))
                        { await State.Club.CancelarReservaAsync(r.Id); await Load(); }
                    }, true));
                _list.Add(Ui.Card(card));
            }
        }
        else { _areas = await State.Club.AreasAsync(); DrawAreas(); }
    }
    private void DrawAreas()
    {
        _list.Clear(); string q = _search.Text?.Trim() ?? "";
        var items = _areas.Where(a => CultureInfo.GetCultureInfo("pt-BR").CompareInfo.IndexOf(a.Nome, q, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0).ToList();
        foreach (var a in items)
        {
            var image = new Image { Source = a.Imagem, HeightRequest = 190, Aspect = Aspect.AspectFill };
            var title = Ui.Text(a.Nome, 24, Ui.Ink, true);
            var text = Ui.Text(a.Descricao, 15, Ui.Muted); text.MaxLines = 3; text.LineBreakMode = LineBreakMode.TailTruncation;
            var info = Ui.Stack(title, text, Ui.Badge($"{a.LimiteTexto} • Até {a.Capacidade} pessoas"), Ui.Text("Consultar agenda  ›", 14, Ui.Teal, true)); info.Padding = 20;
            var card = new Border { Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 24 }, BackgroundColor = Colors.White,
                Content = new VerticalStackLayout { Spacing = 0, Children = { image, info } }, Shadow = new Shadow { Opacity = .06f, Radius = 12, Offset = new Point(0, 4), Brush = Colors.Black } };
            Ui.Tap(card, async () => await Run(() => Go(new frmNovaReserva(State, a)))); _list.Add(card);
        }
        if (items.Count == 0) _list.Add(Ui.Empty("Nenhum recurso encontrado", "Tente procurar por outro nome."));
    }
}
