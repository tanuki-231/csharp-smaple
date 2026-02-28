namespace TodoApi.Domain.Exceptions;

public abstract class AppException(string message) : Exception(message);

public class UnauthorizedException(string message = "Unauthorized") : AppException(message);
public class ForbiddenException(string message = "Forbidden") : AppException(message);
public class SessionTimeoutException(string message = "session timeout") : AppException(message);
public class NotFoundException(string message = "Not Found") : AppException(message);
public class ValidationException(string message) : AppException(message);
