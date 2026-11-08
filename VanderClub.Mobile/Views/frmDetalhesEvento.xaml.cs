using VanderClub.Helpers;
namespace VanderClub.Views;
public sealed partial class frmDetalhesEvento : ClubPage
{
    private readonly int _id;
    public frmDetalhesEvento(AppState state, int id) : base(state, "Detalhes do evento")
    { InitializeComponent(); Body = LayoutPrincipal; _id = id; }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); await Run(async () => {
            var e = await State.Club.EventoAsync(_id); Body.Clear();
            Heading(e.Nome, State.Atual?.Unidade.Nome ?? "");
            Body.Add(Ui.Badge(e.Status, e.Status == "Cancelado" ? Color.FromArgb("#AD3449") : Ui.Teal));
            Body.Add(Ui.Card(Ui.Stack(Ui.Text(e.Descricao, 16), Ui.Text("Local: " + e.Local),
                Ui.Text("Início: " + Horario.DataHora(e.InicioUtc)), Ui.Text("Término: " + Horario.DataHora(e.FimUtc)),
                Ui.Text("Público: " + e.Publico), Ui.Text($"Capacidade: {e.Capacidade} pessoas"))));
            if (e.Status == "Cancelado" && !string.IsNullOrWhiteSpace(e.MotivoCancelamento))
                Body.Add(Ui.Banner("Motivo do cancelamento: " + e.MotivoCancelamento));
            Body.Add(Ui.Text("A programação é organizada pela administração do clube.", 13, Ui.Muted));
        });
    }
}
