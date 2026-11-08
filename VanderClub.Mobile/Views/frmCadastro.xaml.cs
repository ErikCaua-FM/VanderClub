using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmCadastro : ClubPage
{
    private readonly Plano _plan;
    private readonly Entry _nome = Ui.Entry("Nome completo"), _email = Ui.Entry("E-mail", Keyboard.Email), _cpf = Ui.Entry("CPF", Keyboard.Numeric),
        _telefone = Ui.Entry("Celular com DDD", Keyboard.Telephone), _endereco = Ui.Entry("Rua / logradouro"),
        _numero = Ui.Entry("Número"), _bairro = Ui.Entry("Bairro"), _complemento = Ui.Entry("Complemento (opcional)"), _cep = Ui.Entry("CEP", Keyboard.Numeric),
        _cidade = Ui.Entry("Cidade"), _senha = Ui.Entry("Mínimo de 8 caracteres", secret: true), _confirmar = Ui.Entry("Repita a senha", secret: true);
    private readonly DatePicker _nascimento;
    private readonly Picker _unidade = new() { Title = "Selecione a unidade de origem" }, _uf = new() { Title = "UF" }, _pagamento = new() { Title = "Condição de pagamento" };
    private readonly CheckBox _aceite = new() { Color = Ui.Teal };
    private bool _loaded;
    public frmCadastro(AppState s, Plano plan) : base(s, "Complete seu cadastro")
    {
        InitializeComponent(); Body = LayoutPrincipal;
        _plan = plan;
        _nascimento = new DatePicker { Date = s.Hoje.AddYears(-20), MinimumDate = s.Hoje.AddYears(-120), MaximumDate = s.Hoje.AddYears(-18), Format = "dd/MM/yyyy" };
        _uf.ItemsSource = "AC AL AP AM BA CE DF ES GO MA MT MS MG PA PB PR PE PI RJ RN RS RO RR SC SP SE TO".Split(' '); _uf.SelectedItem = "PR";
        _pagamento.ItemsSource = plan.Condicoes; _pagamento.SelectedIndex = 0;
        Heading(plan.Nome, $"Vigência de {plan.DuracaoMeses} meses • Até {plan.LimiteBeneficiarios} convidados/dependentes");
        Body.Add(Ui.Banner("Confira os dados e as condições do plano antes de concluir."));
        Body.Add(Ui.Card(Ui.Stack(Ui.Field("Unidade de origem", _unidade), Ui.Field("Condição de pagamento", _pagamento),
            Ui.Field("Nome completo", _nome), Ui.Field("E-mail de login", _email), Ui.Field("CPF", _cpf), Ui.Field("Nascimento do titular", _nascimento),
            Ui.Field("Celular", _telefone), Ui.Field("Logradouro", _endereco), Ui.Row(Ui.Field("Número", _numero), Ui.Field("CEP", _cep)), Ui.Field("Bairro", _bairro), Ui.Field("Complemento", _complemento), Ui.Row(Ui.Field("Cidade", _cidade), Ui.Field("UF", _uf)),
            Ui.Field("Senha", _senha), Ui.Field("Confirme a senha", _confirmar))));
        var terms = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
        terms.Add(_aceite); terms.Add(Ui.Text("Li as regras: o titular é maior de 18 anos, convidados precisam de aprovação, espaços têm agenda compartilhada e a associação segue as condições do plano.", 13, Ui.Muted), 1, 0);
        Body.Add(terms); Body.Add(Action("Concluir cadastro", Salvar));
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_loaded) return;
        await Run(async () => { _unidade.ItemsSource = await State.Club.UnidadesAsync(); _unidade.SelectedIndex = 0; _loaded = true; });
    }
    private async Task Salvar()
    {
        if (!_aceite.IsChecked) throw new RegraException("Leia e aceite as regras para concluir.");
        if (_senha.Text != _confirmar.Text) throw new RegraException("As senhas não coincidem.");
        if (_unidade.SelectedItem is not Unidade unit) throw new RegraException("Selecione a unidade.");
        if (_pagamento.SelectedItem is not CondicaoPagamento condition) throw new RegraException("Selecione a condição de pagamento.");
        Validacao.Email(_email.Text ?? ""); Validacao.Cpf(_cpf.Text ?? ""); Validacao.Telefone(_telefone.Text ?? "");
        Validacao.Obrigatorio(_numero.Text, "o número"); Validacao.Obrigatorio(_bairro.Text, "o bairro");
        if (Validacao.Digitos(_cep.Text).Length != 8) throw new RegraException("Informe um CEP com 8 dígitos.");
        if ((_senha.Text?.Length ?? 0) < 8) throw new RegraException("Use uma senha de pelo menos 8 caracteres.");
        await State.Club.CadastrarAsync(new Cadastro(_nome.Text ?? "", _email.Text ?? "", _senha.Text ?? "", _cpf.Text ?? "", _nascimento.Date ?? State.Hoje,
            _telefone.Text ?? "", _endereco.Text ?? "", _cidade.Text ?? "", _uf.SelectedItem?.ToString() ?? "", _plan.Id, unit.Id, _plan.VersaoId, condition.Id, _numero.Text ?? "", _bairro.Text ?? "", _complemento.Text ?? "", Validacao.Digitos(_cep.Text), true));
        await DisplayAlertAsync("Cadastro realizado", "Seu cadastro foi salvo. Agora entre com o e-mail e a senha que acabou de criar.", "Ir para o login");
        App.Root(new frmLogin(State, _email.Text?.Trim()));
    }
}
