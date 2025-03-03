using System.Text.Json;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public interface IRunService
{
    Task<Run?> GetRun(Guid id);
    Task<CreateRunResult> CreateRun(User user, long categoryId, JsonDocument request);
}

[GenerateOneOf]
// TODO: May need more cases.
public partial class CreateRunResult : OneOfBase<Run, BadRole, NotFound, Unprocessable>;
