using System.Globalization;
namespace VanderClub.Helpers;
public sealed class MonthCalendar : ContentView
{
    private readonly Grid _days = new() { ColumnSpacing = 3, RowSpacing = 5 };
    private readonly Label _month = Ui.Text("", 18, Ui.Ink, true);
    private readonly DateTime _minDate;
    private HashSet<DateTime> _busy = [];
    public DateTime SelectedDate { get; private set; }
    public DateTime VisibleMonth { get; private set; }
    public event EventHandler<DateTime>? DatePicked;
    public event EventHandler? MonthChanged;
    public MonthCalendar(DateTime date, DateTime minDate)
    {
        _minDate = minDate.Date; SelectedDate = date.Date; VisibleMonth = new DateTime(date.Year, date.Month, 1);
        for (int c = 0; c < 7; c++) _days.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int r = 0; r < 7; r++) _days.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var prev = Ui.Button("‹", true); var next = Ui.Button("›", true);
        prev.WidthRequest = next.WidthRequest = 42; prev.Padding = next.Padding = 0; prev.BackgroundColor = next.BackgroundColor = Colors.Transparent;
        prev.Clicked += (_, _) => Change(-1); next.Clicked += (_, _) => Change(1);
        var top = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        _month.HorizontalTextAlignment = TextAlignment.Center; _month.VerticalTextAlignment = TextAlignment.Center;
        top.Add(prev); top.Add(_month, 1, 0); top.Add(next, 2, 0);
        Content = Ui.Card(Ui.Stack(top, _days, Ui.Text("• Há reservas neste dia. Consulte os horários abaixo.", 11, Ui.Muted)), 12, radius: 24);
        Render();
    }
    public void SetSelected(DateTime date)
    {
        SelectedDate = date.Date;
        var month = new DateTime(date.Year, date.Month, 1); bool changed = month != VisibleMonth; VisibleMonth = month;
        Render(); if (changed) MonthChanged?.Invoke(this, EventArgs.Empty);
    }
    public void SetBusy(IEnumerable<DateTime> dates) { _busy = dates.Select(d => d.Date).ToHashSet(); Render(); }
    private void Change(int delta) { VisibleMonth = VisibleMonth.AddMonths(delta); Render(); MonthChanged?.Invoke(this, EventArgs.Empty); }
    private void Render()
    {
        _days.Clear(); _month.Text = VisibleMonth.ToString("MMMM 'DE' yyyy", CultureInfo.GetCultureInfo("pt-BR")).ToUpperInvariant();
        var labels = new[] { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom" };
        for (int c = 0; c < 7; c++) { var l = Ui.Text(labels[c], 12, Ui.Muted, true); l.HorizontalTextAlignment = TextAlignment.Center; _days.Add(l, c, 0); }
        int offset = ((int)VisibleMonth.DayOfWeek + 6) % 7;
        var first = VisibleMonth.AddDays(-offset);
        for (int i = 0; i < 42; i++)
        {
            var day = first.AddDays(i); bool selected = day == SelectedDate;
            var cell = new Grid { HeightRequest = 44 };
            var b = new Button { Text = day.Day.ToString(), FontSize = 17, Padding = 0, CornerRadius = 21, HeightRequest = 42, MinimumWidthRequest = 0,
                BackgroundColor = selected ? Ui.Teal : Colors.Transparent, TextColor = selected ? Colors.White : day.Month != VisibleMonth.Month || day < _minDate ? Color.FromArgb("#C4C5CD") : Ui.Ink,
                IsEnabled = day >= _minDate };
            b.Clicked += (_, _) => { SetSelected(day); DatePicked?.Invoke(this, day); };
            cell.Add(b);
            if (_busy.Contains(day)) cell.Add(new Label { Text = "•", TextColor = selected ? Colors.White : Ui.Purple, FontSize = 12, VerticalOptions = LayoutOptions.End, HorizontalOptions = LayoutOptions.Center, InputTransparent = true, Margin = new Thickness(0, 0, 0, -2) });
            _days.Add(cell, i % 7, i / 7 + 1);
        }
    }
}
