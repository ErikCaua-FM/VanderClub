using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmVotacoes : ClubPage
{
    private readonly AppState state;
    public frmVotacoes(AppState state) : base(state, "Votações") { InitializeComponent(); Body = LayoutPrincipal; this.state = state; }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); await Run(async () =>
        {
            Body.Clear(); Heading("Sua voz no clube", "Participe das decisões e acompanhe os resultados da unidade.");
            var list = await State.Club.VotacoesAsync();
            if (list.Count == 0) Body.Add(Ui.Empty("Nenhuma votação cadastrada", "As votações serão publicadas pela administração."));
            foreach (var v in list)
            {
                var d = await State.Club.VotacaoAsync(v.Id);
                bool closed = v.Encerrada || State.Clock.UtcNow >= v.FimUtc;
                string status = closed ? "Encerrada" : State.Clock.UtcNow < v.InicioUtc ? "Agendada" : d.MeuVoto is null ? "Pendente" : "Voto registrado";
                var details = Ui.Stack(Ui.Badge(status, closed ? Ui.Muted : Ui.Teal), Ui.Text(v.Motivo, 21, Ui.Ink, true),
                    Ui.Text($"Abertura: {Horario.DataHora(v.InicioUtc)}\nEncerramento: {Horario.DataHora(v.FimUtc)}", 13, Ui.Muted));
                if (closed) details.Add(Ui.Text(Resultado(d), 14, Ui.Purple, true));
                details.Add(Ui.Text(closed ? "Ver resultado  ›" : "Abrir votação  ›", 14, Ui.Teal, true));
                var card = Ui.Card(details); Ui.Tap(card, async () => await Run(() => Go(new frmDetalhesVotacao(State, v.Id)))); Body.Add(card);
            }
            Body.Add(Ui.Text("O voto é exclusivo do titular na unidade de origem. Convidados e dependentes podem consultar, mas não votar.", 13, Ui.Muted));
        });
    }
    public static string Resultado(DetalheVotacao d)
    {
        if (d.Total == 0) return "Nenhum voto registrado.";
        var max = d.Opcoes.Max(o => o.Votos); var names = d.Opcoes.Where(o => o.Votos == max).Select(o => o.Texto).ToArray();
        return names.Length > 1 ? "Empate: " + string.Join(" / ", names) : "Resultado: " + names[0];
    }
}
