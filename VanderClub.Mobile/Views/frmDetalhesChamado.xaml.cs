using VanderClub.Helpers;
namespace VanderClub.Views;
public sealed partial class frmDetalhesChamado : ClubPage
{
    private readonly AppState state;
    private readonly int id;
    public frmDetalhesChamado(AppState state, int id) : base(state, "Detalhes do registro") { InitializeComponent(); Body = LayoutPrincipal; this.state = state; this.id = id; }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); await Run(async () =>
        {
            var c = await State.Club.ChamadoAsync(id); Body.Clear();
            Heading(c.Motivo, c.Protocolo + " • " + Horario.DataHora(c.CriadoUtc));
            if (c.Tipo == "Solicitação") Body.Add(Ui.Badge(c.Status ?? "Pendente", Ui.StatusColor(c.Status ?? "Pendente")));
            Body.Add(Ui.Card(Ui.Stack(Ui.Text("Descrição", 15, Ui.Muted, true), Ui.Text(c.Descricao, 17))));
            Body.Add(Ui.Card(Ui.Stack(Ui.Text("Resposta da administração", 18, Ui.Purple, true),
                Ui.Text(string.IsNullOrWhiteSpace(c.RespostaAdmin) ? "Ainda não há uma resposta para este registro." : c.RespostaAdmin, 16),
                Ui.Text(c.RespondidoUtc is null ? "Acompanhe as atualizações por aqui." : Horario.DataHora(c.RespondidoUtc.Value), 12, Ui.Muted))));
        });
    }
}
