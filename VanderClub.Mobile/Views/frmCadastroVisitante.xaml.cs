using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmCadastroVisitante : ClubPage
{
    public frmCadastroVisitante(AppState s, DateTime dia, Visitante? v = null) : base(s, v is null ? "Novo visitante" : "Dados da visita")
    {
        InitializeComponent(); Body = LayoutPrincipal;
        var past = v is not null && dia.Date < s.Hoje;
        var nome = Ui.Entry("Nome completo"); nome.Text = v?.Nome;
        var doc = Ui.Entry("Número do documento"); doc.Text = v?.Documento;
        var tipo = new Picker { ItemsSource = new[] { "CPF", "RG", "Passaporte" } }; tipo.SelectedItem = v?.TipoDocumento ?? "CPF";
        var phone = Ui.Entry("Celular com DDD", Keyboard.Telephone); phone.Text = v?.Telefone;
        var plate = Ui.Entry("Opcional"); plate.Text = v?.Placa;
        var obs = Ui.Editor("Observações para a entrada (opcional)", 90); obs.Text = v?.Observacao;
        var birth = new DatePicker { Date = v?.Nascimento ?? s.Hoje.AddYears(-20), MaximumDate = s.Hoje, MinimumDate = s.Hoje.AddYears(-120), Format = "dd/MM/yyyy" };
        var date = new DatePicker { Date = past ? dia : dia < s.Hoje ? s.Hoje : dia, MinimumDate = past ? dia : s.Hoje, Format = "dd/MM/yyyy" };
        Heading("Uma visita bem-vinda", "Informe os dados para identificar a pessoa na entrada. A visita não utiliza uma vaga do plano.");
        Body.Add(Ui.Card(Ui.Stack(Ui.Field("Nome completo", nome), Ui.Row(Ui.Field("Tipo", tipo), Ui.Field("Documento", doc)), Ui.Field("Nascimento", birth), Ui.Field("Celular / responsável", phone),
            Ui.Field("Data da visita", date), Ui.Field("Placa do veículo", plate), Ui.Field("Observações", obs))));
        if (past)
        {
            foreach (var input in new View[] { nome, doc, tipo, phone, plate, obs, birth, date }) input.IsEnabled = false;
            Body.Add(Ui.Banner("Registro histórico de uma visita passada. Para uma nova visita, cadastre a pessoa na data desejada."));
            return;
        }
        Body.Add(Action("Salvar visitante", async () =>
        {
            await State.Club.SalvarVisitanteAsync(new Visitante { Id = v?.Id ?? 0, Nome = nome.Text ?? "", TipoDocumento = tipo.SelectedItem?.ToString() ?? "", Documento = doc.Text ?? "", Telefone = phone.Text ?? "",
                Nascimento = birth.Date ?? s.Hoje, Dia = (date.Date ?? s.Hoje).ToString("yyyy-MM-dd"), Placa = plate.Text ?? "", Observacao = obs.Text ?? "" });
            await Saved("Visita cadastrada. Ela aparecerá na lista da data escolhida.");
        }));
    }
}
