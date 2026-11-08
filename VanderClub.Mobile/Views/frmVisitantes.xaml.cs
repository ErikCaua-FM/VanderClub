using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmVisitantes : ClubPage
{
    private readonly DatePicker _date;
    private readonly VerticalStackLayout _items = new() { Spacing = 14 };
    public frmVisitantes(AppState s) : base(s, "Visitantes")
    {
        InitializeComponent(); Body = LayoutPrincipal;
        Heading("Quem vem hoje?", "Consulte e organize as visitas de um dia específico.");
        _date = new DatePicker { Date = s.Hoje, Format = "dd/MM/yyyy" };
        Body.Add(Ui.Field("Data da visita", _date));
        Body.Add(Action("Novo visitante +", () => Go(new frmCadastroVisitante(State, _date.Date ?? State.Hoje)))); Body.Add(_items);
        _date.DateSelected += async (_, _) => await Run(Load);
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Load); }
    private async Task Load()
    {
        var list = await State.Club.VisitantesAsync(_date.Date ?? State.Hoje); _items.Clear();
        if (list.Count == 0) _items.Add(Ui.Empty("Nenhum visitante nesta data", "As visitas ficam vinculadas ao dia e à unidade selecionados."));
        foreach (var v in list)
        {
            var content = Ui.Stack(Ui.Text(v.Nome, 20, Ui.Ink, true), Ui.Badge("Visita prevista"), Ui.Text($"{v.TipoDocumento}: {v.Documento}\nContato: {v.Telefone}"));
            if (!string.IsNullOrWhiteSpace(v.Placa)) content.Add(Ui.Text("Placa: " + v.Placa, 13, Ui.Muted));
            var past = (_date.Date ?? State.Hoje).Date < State.Hoje;
            var edit = Action(past ? "Ver detalhes" : "Ver / editar", () => Go(new frmCadastroVisitante(State, _date.Date ?? State.Hoje, v)), true);
            if (past) content.Add(edit);
            else content.Add(Ui.Row(edit, Action("Cancelar visita", async () =>
            {
                if (await DisplayAlertAsync("Cancelar visita?", $"Remover a previsão de entrada de {v.Nome} nesta data?", "Cancelar visita", "Voltar"))
                { await State.Club.CancelarVisitanteAsync(v.Id); await Load(); }
            }, true)));
            _items.Add(Ui.Card(content));
        }
    }
}
