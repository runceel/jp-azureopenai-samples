using System.ComponentModel.DataAnnotations;

namespace CallCenter.Options;

public class SpeechToTextOptions
{
    [Required]
    public string Region { get; set; } = "";

    [Required]
    public string ResourceId { get; set; } = "";
}
