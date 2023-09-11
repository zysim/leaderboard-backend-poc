using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using NodaTime;

namespace LeaderboardBackend.Services;

public class AccountConfirmationService : IAccountConfirmationService
{
    private readonly ApplicationContext _applicationContext;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly AppConfig _appConfig;

    public AccountConfirmationService(
        ApplicationContext applicationContext,
        IEmailSender emailSender,
        IClock clock,
        IOptions<AppConfig> appConfig
    )
    {
        _applicationContext = applicationContext;
        _emailSender = emailSender;
        _clock = clock;
        _appConfig = appConfig.Value;
    }

    public async Task<AccountConfirmation?> GetConfirmationById(Guid id)
    {
        return await _applicationContext.AccountConfirmations.FindAsync(id);
    }

    public async Task<CreateConfirmationResult> CreateConfirmationAndSendEmail(User user)
    {
        if (user.Role is not UserRole.Registered)
        {
            return new BadRole();
        }

        Instant now = _clock.GetCurrentInstant();

        AccountConfirmation newConfirmation =
            new()
            {
                CreatedAt = now,
                ExpiresAt = now + Duration.FromHours(1),
                UserId = user.Id,
            };

        EntityEntry<AccountConfirmation> entry = _applicationContext.AccountConfirmations.Add(newConfirmation);

        try
        {
            await _emailSender.EnqueueEmailAsync(
                user.Email,
                "Confirm Your Account",
                GenerateAccountConfirmationEmailBody(user, newConfirmation)
            );
        }
        catch
        {
            entry.State = EntityState.Detached;
            // TODO: Log/otherwise handle the fact that the email failed to be queued - zysim
            return new EmailFailed();
        }

        await _applicationContext.SaveChangesAsync();
        return newConfirmation;
    }

    private string GenerateAccountConfirmationEmailBody(User user, AccountConfirmation confirmation)
    {
        UriBuilder builder = new(_appConfig.WebsiteUrl);
        builder.Path = "confirm-account";
        builder.Query = $"code={confirmation.Id.ToUrlSafeBase64String()}";
        return $@"Hi {user.Username},<br/><br/>Click <a href=""{builder.Uri.ToString()}"">here</a> to confirm your account.";
    }
}
