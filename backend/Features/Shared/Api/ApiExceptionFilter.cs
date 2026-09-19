using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreGrid.Api.Features.Shared.Api;

// Single exception -> HTTP status mapping (§5.4), registered globally in
// Program.cs. Only ever sees exceptions a controller didn't already handle
// itself — as feature controllers migrate off their own try/catch blocks
// (Phase 3), more of them reach here; until then this only catches what
// previously fell through to ASP.NET Core's default (an unhandled 500).
// Never writes exception text into the response (NFR-14) — only into the
// server-side log, keyed by the same correlation id the client sees.
public class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var correlationId = context.HttpContext.Items.TryGetValue("CorrelationId", out var id)
            ? id as string
            : context.HttpContext.TraceIdentifier;

        var (statusCode, envelope) = context.Exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound,
                new ErrorEnvelope { Message = ex.Message, Code = "not_found", CorrelationId = correlationId }),

            ValidationException ex => (StatusCodes.Status400BadRequest,
                new ErrorEnvelope
                {
                    Message = ex.Message,
                    Code = "validation_error",
                    Errors = ex.Errors.Count > 0 ? ex.Errors : null,
                    CorrelationId = correlationId
                }),

            BusinessRuleException ex => (StatusCodes.Status422UnprocessableEntity,
                new ErrorEnvelope
                {
                    Message = ex.Message,
                    Code = ex.Code ?? "business_rule_violation",
                    CorrelationId = correlationId,
                    Preconditions = ex.Payload
                }),

            ConflictException ex => (StatusCodes.Status409Conflict,
                new ErrorEnvelope { Message = ex.Message, Code = ex.Code ?? "conflict", CorrelationId = correlationId }),

            ForbiddenException ex => (StatusCodes.Status403Forbidden,
                new ErrorEnvelope { Message = ex.Message, Code = "forbidden", CorrelationId = correlationId, Preconditions = ex.Payload }),

            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                new ErrorEnvelope { Message = "This record was changed by someone else. Reload and try again.", Code = "concurrency_conflict", CorrelationId = correlationId }),

            NpgsqlException { IsTransient: true } => (StatusCodes.Status503ServiceUnavailable,
                new ErrorEnvelope { Message = "The database is temporarily unavailable. Try again shortly.", Code = "transient_failure", CorrelationId = correlationId }),

            TimeoutException => (StatusCodes.Status503ServiceUnavailable,
                new ErrorEnvelope { Message = "The request timed out. Try again shortly.", Code = "transient_failure", CorrelationId = correlationId }),

            _ => (StatusCodes.Status500InternalServerError,
                new ErrorEnvelope { Message = "An unexpected error occurred.", Code = "internal_error", CorrelationId = correlationId })
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(context.Exception, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);
        }
        else
        {
            logger.LogWarning(context.Exception, "{ExceptionType} mapped to {StatusCode}. CorrelationId={CorrelationId}",
                context.Exception.GetType().Name, statusCode, correlationId);
        }

        context.Result = new ObjectResult(envelope) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
