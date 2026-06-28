using System.Net;

namespace Challenge.Domain.Exceptions;

public class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(string accountId)
        : base($"Account '{accountId}' was not found.", "ACCOUNT_NOT_FOUND", HttpStatusCode.NotFound)
    {
    }
}
