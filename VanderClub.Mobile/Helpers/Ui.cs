using Microsoft.Maui.Controls.Shapes;
namespace VanderClub.Helpers;
public static class Ui
{
    public static readonly Color Purple = Color.FromArgb("#484579");
    public static readonly Color Teal = Color.FromArgb("#0087A2");
    public static readonly Color Ink = Color.FromArgb("#30323D");
    public static readonly Color Muted = Color.FromArgb("#818592");
    public static readonly Color Surface = Color.FromArgb("#F5F6F9");
    public static readonly Color Pale = Color.FromArgb("#EFEDF8");
    public static Label Text(string text, double size = 15, Color? color = null, bool bold = false) => new()
    { Text = text, FontSize = size, TextColor = color ?? Ink, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None, LineBreakMode = LineBreakMode.WordWrap };
    public static VerticalStackLayout Stack(params View[] views)
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        foreach (var v in views) stack.Add(v); return stack;
    }
    public static Border Card(View content, double padding = 18, Color? bg = null, double radius = 20) => new()
    {
        Content = content, Padding = padding, BackgroundColor = bg ?? Colors.White,
        Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = radius },
        Shadow = new Shadow { Brush = Colors.Black, Opacity = .06f, Radius = 10, Offset = new Point(0, 3) }
    };
    public static Button Button(string text, bool secondary = false) => new()
    {
        Text = text, BackgroundColor = secondary ? Pale : Teal, TextColor = secondary ? Purple : Colors.White,
        FontSize = 16, FontAttributes = FontAttributes.Bold, CornerRadius = 14, MinimumHeightRequest = 48, Padding = new Thickness(16, 10)
    };
    public static Entry Entry(string hint, Keyboard? keyboard = null, bool secret = false) => new()
    { Placeholder = hint, Keyboard = keyboard ?? Keyboard.Default, IsPassword = secret, HeightRequest = 48, ClearButtonVisibility = secret ? ClearButtonVisibility.Never : ClearButtonVisibility.WhileEditing };
    public static Editor Editor(string hint, double height = 130) => new()
    { Placeholder = hint, HeightRequest = height, AutoSize = EditorAutoSizeOption.TextChanges, MaxLength = 4000, BackgroundColor = Colors.Transparent };
    public static View Field(string name, View input) => Stack(Text(name, 13, Muted, true), Card(input, 10, Surface, 12));
    public static View Banner(string text) => Card(Text(text, 13, Purple), 14, Pale, 14);
    public static View Empty(string title, string description) => Card(Stack(Text(title, 19, Ink, true), Text(description, 14, Muted)), 24);
    public static Border Badge(string text, Color? color = null)
    {
        var c = color ?? Teal;
        return new Border { Content = Text(text, 12, c, true), Padding = new Thickness(10, 5),
            Stroke = Colors.Transparent, BackgroundColor = c.WithAlpha(.09f), StrokeShape = new RoundRectangle { CornerRadius = 12 }, HorizontalOptions = LayoutOptions.Start };
    }
    public static Color StatusColor(string status) => status switch
    {
        "Pago" or "Aprovado" or "Atendida" => Color.FromArgb("#268866"),
        "Atrasado" or "Rejeitado" or "Cancelada" => Color.FromArgb("#C65A61"),
        _ => Color.FromArgb("#B18027")
    };
    public static Grid Row(View left, View right, double gap = 12)
    {
        var g = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = gap };
        g.Add(left, 0, 0); g.Add(right, 1, 0); return g;
    }
    public static void Tap(View view, Action action)
    {
        var gesture = new TapGestureRecognizer(); gesture.Tapped += (_, _) => action(); view.GestureRecognizers.Add(gesture);
    }
    public static View Rule() => new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E8E9EF"), Margin = new Thickness(0, 4) };
}
