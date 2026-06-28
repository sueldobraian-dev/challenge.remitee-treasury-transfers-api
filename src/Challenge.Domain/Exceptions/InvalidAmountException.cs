using System.Net;

namespace Challenge.Domain.Exceptions;

public class InvalidAmountException : DomainException
{
    public InvalidAmountException()
        : base("The transfer amount must be strictly positive.", "INVALID_AMOUNT", HttpStatusCode.BadRequest)
    {
    }
}
