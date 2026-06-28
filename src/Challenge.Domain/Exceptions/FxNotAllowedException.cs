using System.Net;

namespace Challenge.Domain.Exceptions;

public class FxNotAllowedException : DomainException
{
    public FxNotAllowedException()
        : base("FX rate is not allowed when currencies are identical.", "FX_NOT_ALLOWED", HttpStatusCode.BadRequest)
    {
    }
}
