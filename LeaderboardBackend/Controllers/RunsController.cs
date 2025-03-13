using System.Net.Mime;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using LeaderboardBackend.Filters;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using OneOf;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class RunsController(
    IRunService runService,
    ICategoryService categoryService,
    IUserService userService,
    IOptions<JsonOptions> options
    ) : ApiController
{
    [AllowAnonymous]
    [HttpGet("api/run/{id}")]
    [SwaggerOperation("Gets a Run by its ID.", OperationId = "getRun")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404, "The Run with ID `id` could not be found.", typeof(ProblemDetails))]
    public async Task<ActionResult<RunViewModel>> GetRun([FromRoute] Guid id)
    {
        Run? run = await runService.GetRun(id);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(RunViewModel.MapFrom(run));
    }

    [Authorize]
    [HttpPost("/category/{id:long}/runs/create")]
    [SwaggerOperation("Creates a new Run for a Category with ID `id`. This request is restricted to confirmed Users and Administrators.", OperationId = "createRun")]
    [SwaggerOperationFilter(typeof(CustomRequestBodyFilter<CreateRunRequest>))]
    [SwaggerResponse(201)]
    [SwaggerResponse(401, "The client is not logged in.", typeof(ProblemDetails))]
    [SwaggerResponse(400, null, typeof(ValidationProblemDetails))]
    [SwaggerResponse(403, "The requesting User is unauthorized to create Runs.", typeof(ProblemDetails))]
    [SwaggerResponse(404, "The Category with ID `id` could not be found, or has been deleted. Read `title` for more information.", typeof(ProblemDetails))]
    [SwaggerResponse(422, null, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<RunViewModel>> CreateRun(
        [FromRoute] long id,
        [FromServices] IValidator<CreateRunRequest> validator
    )
    {
        GetUserResult res = await userService.GetUserFromClaims(HttpContext.User);

        if (res.TryPickT0(out User user, out OneOf<BadCredentials, UserNotFound> _))
        {
            Category? category = await categoryService.GetCategory(id);

            if (category is null)
            {
                return NotFound(
                    ProblemDetailsFactory
                        .CreateProblemDetails(
                            HttpContext,
                            404,
                            "Category Not Found."
                        )
                    );
            }

            if (category.DeletedAt is not null)
            {
                return NotFound(
                    ProblemDetailsFactory
                        .CreateProblemDetails(
                            HttpContext,
                            404,
                            "Category Is Deleted."
                        )
                    );
            }

            try
            {
                CreateRunRequest? request = await JsonSerializer
                    .DeserializeAsync<CreateRunRequest>(
                        Request.Body,
                        options.Value.JsonSerializerOptions
                    );

                if (request is null)
                {
                    return UnprocessableEntity();
                }

                ValidationResult validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    ModelStateDictionary dictionary = new();
                    validationResult.AddToModelState(dictionary);
                    return UnprocessableEntity(
                        ProblemDetailsFactory.CreateValidationProblemDetails(
                            HttpContext,
                            dictionary
                        )
                    );
                }

                CreateRunResult r = await runService.CreateRun(user, category, request);
                return r.Match<ActionResult>(
                    run =>
                    {
                        CreatedAtActionResult result = CreatedAtAction(
                            nameof(GetRun),
                            new { id = run.Id.ToUrlSafeBase64String() },
                            RunViewModel.MapFrom(run)
                        );
                        return result;
                    },
                    badRole => Forbid(),
                    notFound => NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Category Not Found or Has Been Deleted")),
                    // TODO: This needs to be a ValidationProblemDetails, with `Details` populating `errors`
                    unprocessable => UnprocessableEntity(
                        ProblemDetailsFactory.CreateProblemDetails(
                            HttpContext,
                            null,
                            null,
                            null,
                            unprocessable.Detail
                        )
                    )
                );
            }
            catch (JsonException)
            {
                return UnprocessableEntity();
            }
        }

        return Unauthorized();
    }

    [AllowAnonymous]
    [HttpGet("/api/run/{id}/category")]
    [SwaggerOperation("Gets the category a run belongs to.", OperationId = "getRunCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<CategoryViewModel>> GetCategoryForRun(Guid id)
    {
        Run? run = await runService.GetRun(id);

        if (run is null)
        {
            return NotFound("Run not found");
        }

        Category? category = await categoryService.GetCategoryForRun(run);

        if (category is null)
        {
            return NotFound("Category not found");
        }

        return Ok(CategoryViewModel.MapFrom(category));
    }

    /// <summary>Throws exceptions from JsonSerializer.Deserialize.</summary>
    private CreateRunRequest? ParseRunRequest(string bodyStr)
    {
        try
        {
            return JsonSerializer.Deserialize<CreateTimedRunRequest>(bodyStr, options.Value.JsonSerializerOptions);
        }
        catch
        {
            return JsonSerializer.Deserialize<CreateScoredRunRequest>(bodyStr, options.Value.JsonSerializerOptions);
        }
    }
}
