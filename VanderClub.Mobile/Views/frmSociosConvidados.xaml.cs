using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmSociosConvidados : ClubPage
{
    private readonly AppState state;
    public frmSociosConvidados(AppState state) : base(state, "Sócios-convidados") { InitializeComponent(); Body = LayoutPrincipal; this.state = state; }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Load); }
    private async Task Load()
    {
        var c = await State.Club.ContextoAsync(); var list = await State.Club.BeneficiariosAsync();
        Body.Clear(); Heading("Quem aproveita com você", "Convidados e dependentes incluídos no seu plano.");
        int used = list.Count(b => b.Status is "Aprovado" or "Pendente");
        Body.Add(Ui.Banner($"{used} de {c.Associacao.LimiteBeneficiarios} vagas utilizadas • {c.Associacao.PlanoNome}"));
        var add = Action("Novo sócio-convidado +", () => Go(new frmCadastroConvidado(State))); add.IsEnabled = used < c.Associacao.LimiteBeneficiarios; Body.Add(add);
        if (list.Count == 0) Body.Add(Ui.Empty("Sua lista está vazia", "Cadastre alguém para compartilhar os benefícios do seu plano."));
        foreach (var b in list)
        {
            var card = Ui.Stack(Ui.Text(b.Nome, 20, Ui.Ink, true), Ui.Text(b.Tipo + (b.Parentesco.Length > 0 ? " • " + b.Parentesco : ""), 14, Ui.Muted),
                Ui.Badge(b.Status, Ui.StatusColor(b.Status)), Ui.Text($"Nascimento: {b.Nascimento:dd/MM/yyyy}\nContato: {b.Telefone}", 13, Ui.Muted));
            if (b.ObservacaoAdmin.Length > 0) card.Add(Ui.Text(b.ObservacaoAdmin, 13, Ui.Muted));
            card.Add(Ui.Row(Action("Editar", () => Go(new frmCadastroConvidado(State, b)), true), Action("Excluir", async () =>
            {
                if (await DisplayAlertAsync("Excluir beneficiário?", $"O vínculo de {b.Nome} será removido e a vaga será liberada.", "Excluir", "Cancelar"))
                { await State.Club.ExcluirBeneficiarioAsync(b.Id, b.Versao); await Load(); }
            }, true)));
            Body.Add(Ui.Card(card));
        }
        Body.Add(Ui.Text("Novos cadastros e alterações ficam pendentes de aprovação. A administração da unidade fará a conferência dos dados.", 13, Ui.Muted));
    }
}
