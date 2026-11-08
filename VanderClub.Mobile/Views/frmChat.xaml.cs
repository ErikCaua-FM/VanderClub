using System.Collections.ObjectModel;
using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmChat : ClubPage
{
    private readonly ObservableCollection<Mensagem> _messages = [];
    private readonly CollectionView _list;
    private readonly Editor _text = Ui.Editor("Mensagem", 48);
    private readonly IDispatcherTimer _timer;
    private string _pendingKey = Guid.NewGuid().ToString();
    public frmChat(AppState state) : base(state, "Chat geral")
    {
        InitializeComponent(); Body = LayoutPrincipal;
        int myId = state.Atual?.Perfil.Id ?? 0;
        _list = new CollectionView { ItemsSource = _messages, SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() => new Bubble(myId)), Margin = new Thickness(10, 0) };
        var older = Action("Carregar mensagens anteriores", LoadOlder, true); older.FontSize = 12; older.MinimumHeightRequest = 35;
        var heading = Ui.Stack(Ui.Text(state.Atual?.Unidade.Nome ?? "Unidade", 20, Ui.Purple, true), Ui.Text("Grupo da unidade • associados e administração", 12, Ui.Muted),
            older);
        heading.Padding = new Thickness(18, 16, 18, 10);
        _text.MaximumHeightRequest = 120; _text.MaxLength = 2000;
        var send = Action("Enviar", Send); send.WidthRequest = 85;
        var compose = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 10, Padding = new Thickness(14, 10), BackgroundColor = Colors.White };
        compose.Add(Ui.Card(_text, 6, Ui.Surface, 18)); compose.Add(send, 1, 0);
        var grid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) } };
        grid.Add(heading); grid.Add(_list, 0, 1); grid.Add(compose, 0, 2); Content = grid;
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(15);
        _timer.Tick += async (_, _) => { if (!Running) { _timer.Stop(); await Run(() => Refresh(false)); if (IsVisible) _timer.Start(); } };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(() => Refresh(true)); _timer.Start(); }
    protected override void OnDisappearing() { _timer.Stop(); base.OnDisappearing(); }
    private async Task Refresh(bool scroll)
    {
        var list = await State.Club.MensagensAsync(); bool added = false;
        var ids = _messages.Select(m => m.Id).ToHashSet();
        foreach (var m in list) if (ids.Add(m.Id)) { _messages.Add(m); added = true; }
        if ((scroll || added) && _messages.Count > 0) _list.ScrollTo(_messages.Last(), position: ScrollToPosition.End, animate: true);
    }
    private async Task LoadOlder()
    {
        if (_messages.Count == 0) return;
        var list = await State.Club.MensagensAsync(_messages[0].Id);
        if (list.Count == 0) { await DisplayAlertAsync("Histórico", "Você chegou ao início da conversa.", "OK"); return; }
        foreach (var m in list.AsEnumerable().Reverse()) _messages.Insert(0, m);
    }
    private async Task Send()
    {
        await State.Club.EnviarMensagemAsync(_text.Text ?? "", _pendingKey);
        _text.Text = ""; _pendingKey = Guid.NewGuid().ToString(); await Refresh(true);
    }
    private sealed class Bubble(int myId) : ContentView
    {
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged(); if (BindingContext is not Mensagem m) return;
            bool mine = m.AutorUsuarioId == myId && !m.Administrador;
            var body = Ui.Stack(Ui.Text(m.AutorNome + (string.IsNullOrWhiteSpace(m.ClasseAutor) ? "" : " • " + m.ClasseAutor), 12, m.Administrador ? Ui.Purple : Ui.Teal, true), Ui.Text(m.Texto, 15),
                Ui.Text(Horario.Local(m.CriadoUtc).ToString("dd/MM • HH:mm"), 10, Ui.Muted)); body.Spacing = 6;
            var bubble = Ui.Card(body, 14, m.Administrador ? Ui.Pale : mine ? Color.FromArgb("#DCF3ED") : Colors.White, 17);
            bubble.MaximumWidthRequest = 330; bubble.HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start;
            Margin = new Thickness(4, 5); Content = bubble;
        }
    }
}
