using VanderClub.Helpers;
namespace VanderClub.Views;
public sealed partial class frmChamados : ClubPage
{
    private readonly AppState state;
    private readonly string tipo;
    public frmChamados(AppState state, string tipo) : base(state, tipo == "Solicitação" ? "Solicitações" : "Reclamações") { InitializeComponent(); Body = LayoutPrincipal; this.state = state; this.tipo = tipo; }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); await Run(async () =>
        {
            Body.Clear(); Heading(tipo == "Solicitação" ? "Como podemos ajudar?" : "Sua opinião importa", "Consulte o histórico e toque em um registro para ver os detalhes.");
            Body.Add(Action(tipo == "Solicitação" ? "Nova Solicitação +" : "Nova Reclamação +", () => Go(new frmNovoChamado(State, tipo))));
            var list = await State.Club.ChamadosAsync(tipo);
            if (list.Count == 0) Body.Add(Ui.Empty("Nenhum registro por aqui", "Quando você enviar um registro, ele aparecerá nesta lista."));
            foreach (var c in list)
            {
                var content = Ui.Stack(Ui.Text(c.Protocolo, 12, Ui.Teal, true), Ui.Text(c.Motivo, 20, Ui.Ink, true),
                    Ui.Text(Horario.DataHora(c.CriadoUtc), 13, Ui.Muted));
                if (tipo == "Solicitação") content.Add(Ui.Badge(c.Status ?? "Pendente", Ui.StatusColor(c.Status ?? "Pendente")));
                content.Add(Ui.Text("Ver detalhes  ›", 13, Ui.Teal, true));
                var card = Ui.Card(content); Ui.Tap(card, async () => await Run(() => Go(new frmDetalhesChamado(State, c.Id)))); Body.Add(card);
            }
        });
    }
}
