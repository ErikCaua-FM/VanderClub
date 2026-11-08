namespace VanderClub.Models;

public sealed record DetalheVotacao(Votacao Votacao, IReadOnlyList<ResultadoOpcao> Opcoes, Voto? MeuVoto, bool PodeVotar, string Impedimento)
{
    public int Total => Opcoes.Sum(o => o.Votos);
}
