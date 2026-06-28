using System.Net;

namespace Challenge.Domain.Exceptions;

public class AccountIsFrozenException : DomainException
{
    public AccountIsFrozenException()
        : base("One or both accounts are frozen and cannot participate in transactions.", "ACCOUNT_IS_FROZEN", HttpStatusCode.UnprocessableEntity)
    {
    }
}
