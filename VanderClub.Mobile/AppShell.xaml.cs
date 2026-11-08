namespace VanderClub;
public partial class AppShell : Shell
{
 public AppShell(Page initialPage) { InitializeComponent(); Items.Add(new ShellContent { Content = initialPage }); }
}
