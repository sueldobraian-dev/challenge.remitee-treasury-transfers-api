using System.Net;

namespace Challenge.Domain.Exceptions;

public class IdempotencyException : DomainException
{
    public IdempotencyException(string message)
        : base(message, "DUPLICATE_TRANSFER", HttpStatusCode.Conflict)
    {
    }
}
