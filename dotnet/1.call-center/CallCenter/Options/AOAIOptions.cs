using System.ComponentModel.DataAnnotations;

namespace CallCenter.Options;

public class AOAIOptions
{
    [Required]
    public string Endpoint { get; set; } = "";
    
    [Required]
    public string Model { get; set; } = "";
}
