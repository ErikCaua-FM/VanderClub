using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmPagamento : ClubPage
{
    private readonly string _chave = Guid.NewGuid().ToString();
    public frmPagamento(AppState state, Mensalidade m) : base(state, "Detalhes do pagamento")
    {
        InitializeComponent(); Body = LayoutPrincipal;
        Heading("Parcela " + m.Parcela, "Vencimento em " + m.Vencimento.ToString("dd/MM/yyyy"));
        Body.Add(Ui.Card(Ui.Stack(Ui.Text(Horario.Dinheiro(m.ValorCentavos), 36, Ui.Purple, true),
            Ui.Badge(Horario.Status(m, state.Hoje), Ui.StatusColor(Horario.Status(m, state.Hoje))))));
        if (m.PagoUtc is not null)
        {
            Body.Add(Ui.Text("Pagamento registrado em " + Horario.DataHora(m.PagoUtc.Value), 16, Ui.Teal));
            if (m.PagamentoSimulado) Body.Add(Ui.Banner("Pagamento simulado no Mobile. Nenhum valor foi cobrado."));
        }
        else
        {
            Body.Add(Ui.Banner("Simule o pagamento desta parcela. Ela será marcada como paga no Mobile e na Web, sem cobrança bancária."));
            Body.Add(Action("Simular pagamento", async () =>
            {
                if (!await DisplayAlertAsync("Simular pagamento?", $"A parcela {m.Parcela}, de {Horario.Dinheiro(m.ValorCentavos)}, será marcada como paga no sistema. Nenhum valor será cobrado.", "Simular", "Cancelar")) return;
                await State.Club.SimularPagamentoAsync(m.Id, _chave);
                await Saved("Pagamento simulado registrado. A parcela foi atualizada e já pode ser consultada pela administração.");
            }));
        }
        Body.Add(Ui.Text("A simulação não gera boleto, Pix ou transação bancária.", 14, Ui.Muted));
    }
}
