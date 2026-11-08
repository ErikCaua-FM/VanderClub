using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmMinhaConta : ClubPage
{
    private MinhaConta? _conta;
    public frmMinhaConta(AppState state) : base(state, "Minha Conta")
    {
        InitializeComponent();
        Uf.ItemsSource = "AC AL AP AM BA CE DF ES GO MA MT MS MG PA PB PR PE PI RJ RN RS RO RR SC SP SE TO".Split(' ');
        Nascimento.MinimumDate = state.Hoje.AddYears(-120); Nascimento.MaximumDate = state.Hoje;
    }
    protected override async void OnAppearing() { base.OnAppearing(); if (_conta is null) await Run(Load); }
    private async Task Load()
    {
        _conta = await State.Club.MinhaContaAsync(); var d = _conta.Dados; var a = _conta.Associacao;
        Nome.Text = d.Nome; Email.Text = d.Email; Cpf.Text = d.Cpf; Nascimento.Date = d.Nascimento;
        Telefone.Text = d.Telefone; Endereco.Text = d.Endereco; Numero.Text = d.Numero; Complemento.Text = d.Complemento;
        Bairro.Text = d.Bairro; Cidade.Text = d.Cidade; Uf.SelectedItem = d.Uf; Cep.Text = d.Cep;
        Sexo.Text = d.Sexo; EstadoCivil.Text = d.EstadoCivil; Interesses.Text = d.Interesses;
        PlanoPanel.Clear();
        var plan = Ui.Stack(Ui.Badge(a.PlanoNome, Ui.Purple), Ui.Text($"{_conta.TipoSocio} • {_conta.Status}", 16, Ui.Teal, true),
            Ui.Text("Unidade de origem: " + _conta.UnidadeOrigem),
            Ui.Text("Abrangência: " + (a.TodaRede ? "Toda a rede" : "Unidade de origem")),
            Ui.Text($"Vigência: {a.InicioLocal:dd/MM/yyyy} a {a.FimLocal.AddDays(-1):dd/MM/yyyy}"),
            Ui.Text($"Até {a.LimiteBeneficiarios} convidados/dependentes"),
            Ui.Text("Benefícios: " + a.Beneficios), Ui.Text("Condições: " + a.Regras, 13, Ui.Muted));
        if (_conta.TipoSocio == "Titular")
        {
            plan.Add(Ui.Text($"Total contratado: {Horario.Dinheiro(a.ValorTotalCentavos)} • {a.Parcelas} parcela(s) • {a.Condicao}", 16, Ui.Ink, true));
            plan.Add(Ui.Text($"Versão contratada: {a.VersaoPlano}. Valores e benefícios preservados durante a vigência.", 13, Ui.Muted));
        }
        PlanoPanel.Add(Ui.Card(plan)); SalvarButton.IsEnabled = true;
    }
    private async void Salvar(object? sender, EventArgs e) => await Run(async () =>
    {
        if (_conta is null) return;
        var cep = Validacao.Digitos(Cep.Text);
        if (cep.Length != 8) throw new RegraException("Informe um CEP com 8 dígitos.");
        if (Uf.SelectedItem is not string uf) throw new RegraException("Selecione a UF.");
        var dados = new DadosPessoais {
            Versao = _conta.Dados.Versao, Nome = Validacao.Obrigatorio(Nome.Text, "o nome", 200),
            Email = Validacao.Email(Email.Text ?? ""), Cpf = Validacao.Cpf(Cpf.Text ?? ""),
            Nascimento = Validacao.Nascimento(Nascimento.Date ?? State.Hoje, State.Hoje, _conta.TipoSocio == "Titular"),
            Telefone = Validacao.Telefone(Telefone.Text ?? ""), Endereco = Validacao.Obrigatorio(Endereco.Text, "o logradouro", 250),
            Numero = Validacao.Obrigatorio(Numero.Text, "o número", 20), Complemento = Complemento.Text?.Trim() ?? "",
            Bairro = Validacao.Obrigatorio(Bairro.Text, "o bairro", 100), Cidade = Validacao.Obrigatorio(Cidade.Text, "a cidade", 150),
            Uf = uf, Cep = cep, Sexo = Sexo.Text?.Trim() ?? "", EstadoCivil = EstadoCivil.Text?.Trim() ?? "", Interesses = Interesses.Text?.Trim() ?? "" };
        if (!await DisplayAlertAsync("Salvar seus dados?", "Confira as informações. Caso altere o e-mail, use o novo endereço no próximo login.", "Salvar", "Revisar")) return;
        await State.Club.AtualizarContaAsync(dados);
        await State.AtualizarAsync(); await Load();
        await DisplayAlertAsync("Dados atualizados", "Suas informações foram salvas.", "OK");
    }, SalvarButton);
}
