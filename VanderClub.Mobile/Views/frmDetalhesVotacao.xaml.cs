using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmDetalhesVotacao : ClubPage
{
    private readonly AppState state;
    private readonly int id;
    public frmDetalhesVotacao(AppState state, int id) : base(state, "Detalhes da votação") { InitializeComponent(); Body = LayoutPrincipal; this.state = state; this.id = id; }
    private int? _selected;
    protected override async void OnAppearing() { base.OnAppearing(); await Run(Load); }
    private async Task Load()
    {
        var d = await State.Club.VotacaoAsync(id); var v = d.Votacao; bool closed = v.Encerrada || State.Clock.UtcNow >= v.FimUtc;
        Body.Clear(); Heading(v.Motivo, $"Até {Horario.DataHora(v.FimUtc)}"); Body.Add(Ui.Card(Ui.Text(v.Descricao, 16)));
        if (d.MeuVoto is not null) Body.Add(Ui.Banner("Seu voto foi registrado. Obrigado por participar!"));
        else if (!d.PodeVotar) Body.Add(Ui.Banner(d.Impedimento));
        if (d.PodeVotar)
        {
            var choices = Ui.Stack(Ui.Text("Escolha uma opção", 20, Ui.Ink, true));
            foreach (var o in d.Opcoes)
            {
                var rb = new RadioButton { Content = o.Texto, GroupName = "votacao_" + id, Value = o.Id, TextColor = Ui.Ink, FontSize = 16 };
                rb.CheckedChanged += (_, e) => { if (e.Value) _selected = o.Id; }; choices.Add(rb);
            }
            Body.Add(Ui.Card(choices));
            Body.Add(Action("Confirmar meu voto", async () =>
            {
                if (_selected is null) throw new RegraException("Selecione uma opção para votar.");
                var option = d.Opcoes.First(o => o.Id == _selected);
                if (!await DisplayAlertAsync("Confirmar voto?", $"Você escolheu: {option.Texto}.\nDepois de confirmar, não será possível alterar.", "Confirmar voto", "Revisar")) return;
                await State.Club.VotarAsync(id, _selected.Value); await Load();
            }));
        }
        if (closed || d.MeuVoto is not null)
        {
            var results = Ui.Stack(Ui.Text(closed ? "Apuração final" : "Apuração parcial", 22, Ui.Purple, true), Ui.Text($"{d.Total} voto(s) registrado(s)", 13, Ui.Muted));
            foreach (var o in d.Opcoes)
            {
                double percent = d.Total == 0 ? 0 : (double)o.Votos / d.Total;
                results.Add(Ui.Text($"{o.Texto} • {o.Votos} ({percent:P0})", 15));
                results.Add(new ProgressBar { Progress = percent, ProgressColor = Ui.Teal, BackgroundColor = Ui.Pale, HeightRequest = 9 });
            }
            if (closed) results.Add(Ui.Text(frmVotacoes.Resultado(d), 17, Ui.Ink, true));
            Body.Add(Ui.Card(results));
        }
        else Body.Add(Ui.Text("A apuração parcial ficará disponível depois do seu voto. Os resultados finais serão exibidos ao encerrar.", 13, Ui.Muted));
    }
}
