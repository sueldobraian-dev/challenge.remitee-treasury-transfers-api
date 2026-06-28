using System.Net;

namespace Challenge.Domain.Exceptions;

public class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(string requested, string actual)
        : base($"The requested currency '{requested}' does not match the source account currency '{actual}'.", "CURRENCY_MISMATCH", HttpStatusCode.BadRequest)
    {
    }
}
