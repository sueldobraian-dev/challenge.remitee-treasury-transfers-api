using System.Net;

namespace Challenge.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string Code { get; }
    public HttpStatusCode StatusCode { get; }

    protected DomainException(string message, string code, HttpStatusCode statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}
