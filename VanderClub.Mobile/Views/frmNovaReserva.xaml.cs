using VanderClub.Helpers;
using VanderClub.Models;
namespace VanderClub.Views;
public sealed partial class frmNovaReserva : ClubPage
{
    private readonly Area _area;
    private readonly MonthCalendar _calendar;
    private readonly DatePicker _entrada, _saida;
    private readonly TimePicker _inicio, _fim;
    private readonly VerticalStackLayout _occupied = new() { Spacing = 8 };
    private readonly Label _selected = Ui.Text("", 18, Ui.Purple, true), _duration = Ui.Text("", 14, Ui.Teal, true);
    private readonly CheckBox _terms = new() { Color = Ui.Teal };
    private readonly string _key = Guid.NewGuid().ToString();
    private bool _updating;
    public frmNovaReserva(AppState state, Area area) : base(state, area.Nome)
    {
        InitializeComponent(); Body = LayoutPrincipal;
        _area = area;
        var now = Horario.Local(state.Clock.UtcNow).AddHours(1);
        var date = now.Date; var time = TimeSpan.FromHours(Math.Max(8, now.Hour));
        if (time.TotalHours >= 21 && area.MaxMinutos > 0) { date = date.AddDays(1); time = TimeSpan.FromHours(8); }
        _entrada = new DatePicker { Date = date, MinimumDate = state.Hoje, Format = "dd/MM/yyyy" };
        var end = date.Add(time).AddHours(1);
        _saida = new DatePicker { Date = end.Date, MinimumDate = state.Hoje, Format = "dd/MM/yyyy" };
        _inicio = new TimePicker { Time = time, Format = "HH:mm" }; _fim = new TimePicker { Time = end.TimeOfDay, Format = "HH:mm" };
        _calendar = new MonthCalendar(date, state.Hoje);
        Body.Add(Ui.Text("Unidade: " + state.Atual?.Unidade.Nome, 14, Ui.Muted));
        Body.Add(_calendar); Body.Add(Ui.Card(Ui.Stack(Ui.Text("Dia selecionado", 13, Ui.Muted), _selected), 18, Ui.Pale));
        Body.Add(Ui.Text("Horários já ocupados", 18, Ui.Ink, true)); Body.Add(_occupied);
        Body.Add(Ui.Card(Ui.Stack(Ui.Text("Sua reserva", 22, Ui.Ink, true), Ui.Badge(area.LimiteTexto),
            Ui.Row(Ui.Field("Data de entrada", _entrada), Ui.Field("Data de saída", _saida)), Ui.Row(Ui.Field("Horário inicial", _inicio), Ui.Field("Horário final", _fim)), _duration)));
        Body.Add(Action("Ler termos de uso", async () => await DisplayAlertAsync(area.Nome, area.Regras, "Entendi"), true));
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
        row.Add(_terms); var termText = Ui.Text("Li as regras de uso e concordo com os horários e a capacidade do espaço.", 13, Ui.Muted); termText.VerticalOptions = LayoutOptions.Center; row.Add(termText, 1, 0); Body.Add(row);
        Body.Add(Action("Confirmar reserva", Save));
        _calendar.DatePicked += async (_, day) => { _updating = true; _entrada.Date = day; if ((_saida.Date ?? day) < day) _saida.Date = day; _updating = false; UpdateDuration(); await Run(LoadCalendar); };
        _calendar.MonthChanged += async (_, _) => await Run(LoadCalendar);
        _entrada.DateSelected += async (_, _) =>
        {
            if (_updating) return; _updating = true;
            var d = _entrada.Date ?? State.Hoje; if ((_saida.Date ?? d) < d) _saida.Date = d; _calendar.SetSelected(d);
            _updating = false; UpdateDuration(); await Run(LoadCalendar);
        };
        _saida.DateSelected += (_, _) => UpdateDuration();
        _inicio.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) UpdateDuration(); };
        _fim.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) UpdateDuration(); };
        UpdateDuration();
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Run(LoadCalendar); }
    private (DateTime Start, DateTime End) Interval() => ((_entrada.Date ?? State.Hoje).Date + (_inicio.Time ?? TimeSpan.Zero), (_saida.Date ?? State.Hoje).Date + (_fim.Time ?? TimeSpan.Zero));
    private void UpdateDuration()
    {
        var (start, end) = Interval(); var span = end - start;
        _duration.Text = span > TimeSpan.Zero ? $"Duração: {(int)span.TotalHours}h {span.Minutes:00}min" : "A saída precisa ser posterior à entrada.";
        _duration.TextColor = span <= TimeSpan.Zero || (_area.MaxMinutos > 0 && span.TotalMinutes > _area.MaxMinutos) ? Ui.StatusColor("Atrasado") : Ui.Teal;
    }
    private async Task LoadCalendar()
    {
        var month = _calendar.VisibleMonth;
        var monthBookings = await State.Club.AgendaAsync(_area.Id, month.AddDays(-7), month.AddMonths(1).AddDays(7));
        var dates = new HashSet<DateTime>();
        foreach (var r in monthBookings)
        {
            var first = Horario.Local(r.InicioUtc).Date; var last = Horario.Local(r.FimUtc).AddTicks(-1).Date;
            for (var d = first < month.AddDays(-7) ? month.AddDays(-7) : first; d <= last && d < month.AddMonths(1).AddDays(7); d = d.AddDays(1)) dates.Add(d);
        }
        _calendar.SetBusy(dates);
        var selected = (_entrada.Date ?? State.Hoje).Date; _selected.Text = selected.ToString("dd/MM/yyyy");
        var bookings = await State.Club.AgendaAsync(_area.Id, selected, selected.AddDays(1)); _occupied.Clear();
        if (bookings.Count == 0) _occupied.Add(Ui.Text("Nenhuma reserva neste dia. Confira as regras e escolha seu horário.", 14, Ui.Muted));
        foreach (var r in bookings) _occupied.Add(Ui.Banner($"Ocupado: {Horario.DataHora(r.InicioUtc)} → {Horario.DataHora(r.FimUtc)}"));
    }
    private async Task Save()
    {
        if (!_terms.IsChecked) throw new RegraException("Leia e aceite os termos de uso antes de reservar.");
        var (start, end) = Interval();
        if (!await DisplayAlertAsync("Confirmar reserva?", $"{_area.Nome}\nEntrada: {start:dd/MM/yyyy HH:mm}\nSaída: {end:dd/MM/yyyy HH:mm}", "Confirmar", "Revisar")) return;
        var r = await State.Club.ReservarAsync(_area.Id, start, end, _key);
        await Saved($"Reserva {r.Protocolo} confirmada. Ela está disponível em Minhas reservas.");
    }
}
