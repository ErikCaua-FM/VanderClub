using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;
using VanderClub.Api.Requests;

namespace VanderClub.Api.Services;
public sealed partial class ClubeService
{
    public Task<object> SaveArea(Trabalho w, Ator a, int id, AreaInput d)
    {
        a.ExigirGeral();
        throw new ApiException(409, "As áreas são padronizadas no código e não precisam de cadastro no banco.");
    }
}
