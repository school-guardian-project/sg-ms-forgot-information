using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ms_forgot_information.Api.Shared.Domain.Exceptions;

namespace ms_forgot_information.Api.Shared.Infrastructure.Middleware;

/// <summary>
/// Maps domain exceptions to HTTP responses without ever leaking stack traces, upstream
/// error details, or anything about whether a given account exists.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (TooManyRequestsException ex)
        {
            await WriteAsync(context, HttpStatusCode.TooManyRequests, ex.Message);
        }
        catch (TooManyAttemptsException ex)
        {
            await WriteAsync(context, HttpStatusCode.TooManyRequests, ex.Message);
        }
        catch (UpstreamUpdateException ex)
        {
            logger.LogError(ex.Inner, "Upstream update failed");
            await WriteAsync(context, HttpStatusCode.BadGateway, "No se pudo completar la actualización. Intenta de nuevo.");
        }
        catch (NotificationDeliveryException)
        {
            await WriteAsync(context, HttpStatusCode.ServiceUnavailable, "No se pudo enviar el código. Intenta de nuevo más tarde.");
        }
        catch (EmailAlreadyInUseException ex)
        {
            await WriteAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (VerificationException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.");
        }
    }

    private static Task WriteAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
    }
}
