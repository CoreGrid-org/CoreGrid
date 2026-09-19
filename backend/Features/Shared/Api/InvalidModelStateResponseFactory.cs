using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CoreGrid.Api.Features.Shared.Api;

// Reshapes [ApiController]'s automatic ModelState-invalid 400 (produced
// before a controller action ever runs) into the same ErrorEnvelope every
// other 4xx/5xx uses (§5.4, NFR-11). Registered as
// ApiBehaviorOptions.InvalidModelStateResponseFactory in Program.cs — a
// wire addition, not a wire break: `message` stays present, so the
// frontend's existing getErrorMessage() needs no change (§7).
public static class InvalidModelStateResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var correlationId = context.HttpContext.Items.TryGetValue("CorrelationId", out var id)
            ? id as string
            : context.HttpContext.TraceIdentifier;

        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var envelope = new ErrorEnvelope
        {
            Message = "One or more fields are invalid.",
            Code = "validation_error",
            Errors = errors.Count > 0 ? errors : null,
            CorrelationId = correlationId
        };

        return new BadRequestObjectResult(envelope);
    }
}
