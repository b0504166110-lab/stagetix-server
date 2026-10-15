using System.ComponentModel.DataAnnotations;

namespace StageTix.Core.Options;

/// <summary>Size of the auditorium. Bound from the "Hall" configuration section.</summary>
public class HallOptions
{
    public const string SectionName = "Hall";

    [Range(1, 30)]
    public int Rows { get; set; } = 10;

    [Range(1, 40)]
    public int SeatsPerRow { get; set; } = 12;
}
