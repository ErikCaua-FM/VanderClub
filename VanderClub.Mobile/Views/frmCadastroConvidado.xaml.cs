using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmCadastroConvidado : ClubPage
{
    private readonly int _id, _versao;
    private readonly Entry _nome = Ui.Entry("Nome completo"), _cpf = Ui.Entry("CPF", Keyboard.Numeric), _telefone = Ui.Entry("Celular com DDD", Keyboard.Telephone), _parentesco = Ui.Entry("Ex.: filho, cônjuge, amigo");
    private readonly Picker _tipo = new() { ItemsSource = new[] { "Convidado", "Dependente" }, Title = "Tipo" };
    private readonly DatePicker _nascimento;
    public frmCadastroConvidado(AppState state, Beneficiario? b = null) : base(state, b is null ? "Novo sócio-convidado" : "Editar beneficiário")
    {
        InitializeComponent(); Body = LayoutPrincipal;
        _id = b?.Id ?? 0; _versao = b?.Versao ?? 0; _nome.Text = b?.Nome; _cpf.Text = b?.Cpf; _telefone.Text = b?.Telefone; _parentesco.Text = b?.Parentesco;
        _tipo.SelectedItem = b?.Tipo ?? "Convidado";
        _nascimento = new DatePicker { Date = b?.Nascimento ?? state.Hoje.AddYears(-20), MaximumDate = state.Hoje, MinimumDate = state.Hoje.AddYears(-120), Format = "dd/MM/yyyy" };
        Heading("Dados do beneficiário", "Todos os cadastros passam por conferência da administração.");
        Body.Add(Ui.Card(Ui.Stack(Ui.Field("Tipo de vínculo", _tipo), Ui.Field("Nome completo", _nome), Ui.Field("CPF", _cpf), Ui.Field("Nascimento", _nascimento), Ui.Field("Celular ou telefone do responsável", _telefone), Ui.Field("Parentesco / relação", _parentesco))));
        Body.Add(Action("Salvar para aprovação", async () =>
        {
            await State.Club.SalvarBeneficiarioAsync(new Beneficiario { Id = _id, Versao = _versao, Nome = _nome.Text ?? "", Cpf = _cpf.Text ?? "", Telefone = _telefone.Text ?? "", Nascimento = _nascimento.Date ?? State.Hoje,
                Tipo = _tipo.SelectedItem?.ToString() ?? "", Parentesco = _parentesco.Text ?? "" });
            await Saved("Cadastro enviado. Aguarde a conferência da administração.");
        }));
    }
}
