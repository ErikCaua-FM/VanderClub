using System.ComponentModel.DataAnnotations;

namespace VanderClub.Api.Requests;
// Complemento administrativo: o portal atual ainda nao possui formulario de areas.
public sealed class AreaInput
{
    [Required(AllowEmptyStrings = true)]
    public string UnitId { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string TypeCode { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Name { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Description { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Rules { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Image { get; set; } = "";
    public int Capacity { get; set; }
    public int MaxMinutes { get; set; }
    public int OpenMinute { get; set; } = 480;
    public int CloseMinute { get; set; } = 1320;
    public int AdvanceHours { get; set; }
    public int AdvanceDays { get; set; } = 90;
    public int BufferMinutes { get; set; }
    public int CancelHours { get; set; } = 24;
    public bool Active { get; set; } = true;
    public int Version { get; set; }
}
