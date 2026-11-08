using VanderClub.Helpers;
namespace VanderClub.Views;
public sealed partial class frmNovoChamado : ClubPage
{
    public frmNovoChamado(AppState state, string tipo) : base(state, "Nova " + tipo.ToLowerInvariant())
    {
        InitializeComponent(); Body = LayoutPrincipal;
        var motivo = Ui.Entry("Resuma o assunto em uma frase"); motivo.MaxLength = 150;
        var descricao = Ui.Editor("Conte os detalhes para a administração", 180);
        Heading(tipo == "Solicitação" ? "Envie sua solicitação" : "Registre sua reclamação", "O número e a data serão preenchidos automaticamente.");
        Body.Add(Ui.Card(Ui.Stack(Ui.Field("Motivo", motivo), Ui.Field("Descrição", descricao))));
        Body.Add(Action("Enviar " + tipo.ToLowerInvariant(), async () =>
        {
            var id = await State.Club.CriarChamadoAsync(tipo, motivo.Text ?? "", descricao.Text ?? "");
            var c = await State.Club.ChamadoAsync(id);
            await Saved($"Registro {c.Protocolo} enviado e salvo no histórico.");
        }));
    }
}
