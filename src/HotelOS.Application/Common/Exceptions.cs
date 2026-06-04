namespace HotelOS.Application.Common;

/// <summary>Thrown when a requested entity does not exist (maps to HTTP 404).</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, object key)
        : base($"{entity} '{key}' was not found.") { }
}

/// <summary>Thrown on a business rule conflict, e.g. double booking (maps to HTTP 409).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Thrown when the current user is not allowed to perform an action (HTTP 403).</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>Thrown on failed authentication (HTTP 401).</summary>
public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}
