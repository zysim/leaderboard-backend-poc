using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents an account recovery attempt for a `User`.
/// </summary>
public class AccountRecovery
{
    /// <summary>
    ///     The unique identifier of the `AccountRecovery`.<br/>
    ///     Generated on creation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The ID of the `User` tied to this `AccountRecovery`.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    ///     The `User` relationship model.
    /// </summary>
    [Required]
    public User User { get; set; } = null!;

    /// <summary>
    ///     The time this `AccountRecovery` was created, i.e. the time the user
    ///     requested an account recovery.
    /// </summary>
    [Required]
    public Instant CreatedAt { get; set; }

    /// <summary>
    ///     The time this `AccountRecovery` was used.
    /// </summary>
    public Instant? UsedAt { get; set; }

    /// <summary>
    ///     The time this `AccountRecovery` expires. Defaults to an hour from its
    ///     creation.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required]
    public Instant ExpiresAt { get; set; }
}
