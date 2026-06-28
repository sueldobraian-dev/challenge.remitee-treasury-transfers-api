using System.Net;

namespace Challenge.Domain.Exceptions;

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException()
        : base("The source account does not have sufficient funds to complete this transfer.", "INSUFFICIENT_FUNDS", HttpStatusCode.UnprocessableEntity)
    {
    }
}
