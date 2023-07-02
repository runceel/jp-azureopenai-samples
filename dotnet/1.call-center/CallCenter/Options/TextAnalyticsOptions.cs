using System.ComponentModel.DataAnnotations;

namespace CallCenter.Options;

public class TextAnalyticsOptions
{
    [Required]
    public string Endpoint { get; set; } = "";
}
