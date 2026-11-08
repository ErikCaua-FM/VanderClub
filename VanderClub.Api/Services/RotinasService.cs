using VanderClub.Api.Infrastructure;

namespace VanderClub.Api.Services;
public sealed class RotinasService(Banco banco, ClubeService service, ILogger<RotinasService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        int cycles = 0;
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await using var w = await banco.Abrir(stop);
                await w.Begin();
                await service.TickPolls(w);
                if (++cycles % 720 == 0)
                {
                    await w.Exec("DELETE FROM requisicoes_idempotentes WHERE expira_em<UTC_TIMESTAMP(6)");
                    await w.Exec("DELETE FROM sessoes WHERE expira_em<UTC_TIMESTAMP(6)-INTERVAL 30 DAY");
                }

                await w.Commit();
            }
            catch (OperationCanceledException)when (stop.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Rotinas aguardando banco/configuração ({Tipo}). Confira /api/v1/health.", ex.GetType().Name);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stop))
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
