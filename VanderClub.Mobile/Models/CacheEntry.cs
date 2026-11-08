using SQLite;
namespace VanderClub.Models;
[Table("CatalogCache")]
public sealed class CacheEntry
{
    [PrimaryKey] public string Key { get; set; } = "";
    public string Json { get; set; } = "";
    public long ExpiresUtcTicks { get; set; }
}
