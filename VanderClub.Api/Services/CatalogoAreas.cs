using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;

namespace VanderClub.Api.Services;

// Não cadastra áreas no MySQL. IDs estáveis, compartilhados com o catálogo do Mobile.
public static class CatalogoAreas
{
    public sealed record Item(int Id, string Codigo, string Nome, string Imagem, int Capacidade, int MaxMinutos, string Descricao, string Regras);
    public static IReadOnlyList<Item> Todos { get; } = Array.AsReadOnly(new Item[]
    {
        new(1, "ACADEMIA", "Academia", "academia.png", 20, 180, "Equipamentos para musculação e condicionamento físico. Utilize roupas adequadas, higienize os equipamentos e respeite os demais associados.", "Equipamentos para musculação e condicionamento físico. Utilize roupas adequadas, higienize os equipamentos e respeite os demais associados. Funcionamento: 08h às 22h. Duração máxima: 3 horas por reserva."),
        new(2, "FUTEBOL", "Campo de Futebol", "futebol.png", 22, 180, "Campo para partidas e treinos em equipe. O responsável deve organizar os participantes e preservar o gramado e os equipamentos.", "Campo para partidas e treinos em equipe. O responsável deve organizar os participantes e preservar o gramado e os equipamentos. Funcionamento: 08h às 22h. Duração máxima: 3 horas por reserva."),
        new(3, "POLIESPORTIVA", "Quadra Poliesportiva", "poli.png", 16, 180, "Espaço para futsal, basquete e vôlei. Use calçado apropriado, cuide das redes e mantenha a quadra limpa.", "Espaço para futsal, basquete e vôlei. Use calçado apropriado, cuide das redes e mantenha a quadra limpa. Funcionamento: 08h às 22h. Duração máxima: 3 horas por reserva."),
        new(4, "AREIA", "Quadra de Areia", "areia.png", 8, 180, "Quadra para vôlei de areia e beach tennis. Não entre com objetos de vidro e mantenha a areia livre de resíduos.", "Quadra para vôlei de areia e beach tennis. Não entre com objetos de vidro e mantenha a areia livre de resíduos. Funcionamento: 08h às 22h. Duração máxima: 3 horas por reserva."),
        new(5, "TENIS", "Quadra de Tênis", "tenis.png", 4, 180, "Quadra para partidas de simples ou duplas. Utilize calçado adequado e preserve a superfície, a rede e os acessórios.", "Quadra para partidas de simples ou duplas. Utilize calçado adequado e preserve a superfície, a rede e os acessórios. Funcionamento: 08h às 22h. Duração máxima: 3 horas por reserva."),
        new(6, "JOGOS", "Sala de Jogos", "jogos.png", 12, 180, "Mesas e jogos para momentos de convivência. Cuide das peças, respeite os outros jogadores e devolva os materiais ao final.", "Mesas e jogos para momentos de convivência. Cuide das peças, respeite os outros jogadores e devolva os materiais ao final. Funcionamento: 08h às 22h. Duração máxima: 3 horas por reserva."),
        new(7, "SALAO_SOCIAL", "Salão Social", "social.png", 150, 0, "Ambiente para reuniões e confraternizações. Respeite a capacidade, as regras de som e a limpeza na devolução do espaço.", "Ambiente para reuniões e confraternizações. Respeite a capacidade, as regras de som e a limpeza na devolução do espaço. Duração livre, conforme a disponibilidade da agenda."),
        new(8, "SALAO_FESTA", "Salão de Festa", "festa.png", 80, 0, "Espaço para aniversários e celebrações. O responsável acompanha o evento, respeita a capacidade e entrega o salão organizado.", "Espaço para aniversários e celebrações. O responsável acompanha o evento, respeita a capacidade e entrega o salão organizado. Duração livre, conforme a disponibilidade da agenda."),
        new(9, "QUIOSQUE", "Quiosque e Churrasqueira", "quiosque.png", 12, 180, "Área para encontros e refeições. Utilize a churrasqueira com cuidado, descarte os resíduos e deixe o espaço limpo.", "Área para encontros e refeições. Utilize a churrasqueira com cuidado, descarte os resíduos e deixe o espaço limpo. Funcionamento: 08h às 22h. Duração máxima: 3 horas por reserva."),
    });
    public static Registro Obter(int unidadeId, int id)
    {
        var a = Todos.FirstOrDefault(x => x.Id == id) ?? throw new ApiException(422, "Área de reserva inválida.");
        return new Registro(new Dictionary<string, object?>
        {
            ["id"] = a.Id, ["clube_id"] = unidadeId, ["padrao"] = true,
            ["nome"] = a.Nome, ["descricao"] = a.Descricao, ["regras"] = a.Regras,
            ["imagem"] = a.Imagem, ["capacidade"] = a.Capacidade, ["max_minutos"] = a.MaxMinutos,
            ["abre_minuto"] = a.MaxMinutos == 0 ? 0 : 480,
            ["fecha_minuto"] = a.MaxMinutos == 0 ? 1440 : 1320,
            ["antecedencia_minima_horas"] = 0, ["antecedencia_maxima_dias"] = 90,
            ["intervalo_apos_minutos"] = 0, ["cancelamento_minimo_horas"] = 24
        });
    }
}
