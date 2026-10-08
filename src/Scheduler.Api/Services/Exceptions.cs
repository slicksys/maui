namespace Scheduler.Api.Services;

public class ValidationException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);
