using VanderClub.Helpers;
using VanderClub.Views;
namespace VanderClub;
public partial class MainPage : ContentPage
{
 private readonly AppState _state; private bool _started;
 public MainPage(AppState state) { InitializeComponent(); _state = state; }
 protected override async void OnAppearing()
 {
  base.OnAppearing(); if (_started) return; _started = true;
  try { bool logged = await _state.IniciarAsync(); App.Root(logged ? new frmInicio(_state) : new frmLogin(_state)); }
  catch { await _state.EncerrarLocalAsync(); App.Root(new frmLogin(_state)); }
 }
}
