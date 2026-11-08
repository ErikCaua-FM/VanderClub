using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmPlanos : ClubPage
{
    private readonly AppState state;
    public frmPlanos(AppState state) : base(state, "Escolha seu plano") { InitializeComponent(); Body = LayoutPrincipal; this.state = state; }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); await Run(async () =>
        {
            Body.Clear(); Heading("Seu próximo bom momento", "Escolha como aproveitar o VanderClub.");
            var plans = await State.Club.PlanosAsync();
            if (plans.Count == 0) Body.Add(Ui.Empty("Novos planos em breve", "Consulte a administração para saber quando novas adesões estarão disponíveis."));
            foreach (var p in plans)
            {
                var title = Ui.Stack(Ui.Badge(p.TodaRede ? "TODAS AS UNIDADES" : "UMA UNIDADE"),
                    Ui.Text(p.Nome, 25, Ui.Purple, true), Ui.Text(p.Descricao, 15, Ui.Muted),
                    Ui.Text($"Vigência: {p.DuracaoMeses} meses", 17, Ui.Teal, true),
                    Ui.Text($"Até {p.LimiteBeneficiarios} convidados/dependentes", 15));
                foreach (var c in p.Condicoes) title.Add(Ui.Text(c.ToString(), 16, Ui.Ink, true));
                title.Add(Ui.Text(p.Beneficios, 14)); title.Add(Ui.Text(p.Regras, 13, Ui.Muted));
                if (p.Condicoes.Count > 0) title.Add(Action("Quero este plano", () => Go(new frmCadastro(State, p))));
                Body.Add(Ui.Card(title, 22));
            }
            Body.Add(Ui.Text("Visitantes de um dia não consomem a cota de convidados. Nenhum pagamento real é realizado nesta versão.", 13, Ui.Muted));
        });
    }
}
