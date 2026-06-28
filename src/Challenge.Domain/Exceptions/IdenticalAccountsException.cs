using System.Net;

namespace Challenge.Domain.Exceptions;

public class IdenticalAccountsException : DomainException
{
    public IdenticalAccountsException()
        : base("Source and target accounts must be different.", "IDENTICAL_ACCOUNTS", HttpStatusCode.BadRequest)
    {
    }
}
