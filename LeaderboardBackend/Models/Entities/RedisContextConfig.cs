using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Entities;

public class RedisContextConfig : IValidatableObject
{
    public const string KEY = "RedisContext";

    public RedisConfig? Rd { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Rd == null)
        {
            yield return new ValidationResult(
                "Missing Redis configuration.",
                new[] { nameof(Rd) }
            );
        }
    }
}
public class RedisConfig
{
    [Required]
    public required string Host { get; set; }

    [Required]
    public required ushort Port { get; set; }

    [Required]
    public required string User { get; set; }

    [Required]
    public required string Password { get; set; }
}
