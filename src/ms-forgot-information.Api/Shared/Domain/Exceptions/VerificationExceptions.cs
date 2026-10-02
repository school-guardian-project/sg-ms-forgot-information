namespace ms_forgot_information.Api.Shared.Domain.Exceptions;

public abstract class VerificationException(string message) : Exception(message);

public sealed class InvalidCodeException() : VerificationException("El código de verificación es inválido.");

public sealed class CodeExpiredException() : VerificationException("El código de verificación ha expirado.");

public sealed class TooManyAttemptsException() : VerificationException("Se superó el número máximo de intentos. Solicita un nuevo código.");

public sealed class TooManyRequestsException() : VerificationException("Se han solicitado demasiados códigos. Intenta de nuevo más tarde.");

public sealed class InvalidResetTokenException() : VerificationException("El token de restablecimiento es inválido o expiró.");

public sealed class DuplicateContactException(string message) : VerificationException(message);

public sealed class InvalidCredentialsException() : VerificationException("La contraseña actual no es correcta.");

public sealed class WeakPasswordException() : VerificationException(
    "La contraseña debe tener mínimo 8 caracteres, con mayúscula, minúscula, número y carácter especial.");

public sealed class PasswordMismatchException() : VerificationException("Las contraseñas no coinciden.");

public sealed class InvalidContactFormatException(string message) : VerificationException(message);

public sealed class UpstreamUpdateException(string message, Exception inner)
    : VerificationException(message is { Length: > 0 } ? message : "No se pudo completar la actualización. Intenta de nuevo.")
{
    public Exception Inner { get; } = inner;
}
