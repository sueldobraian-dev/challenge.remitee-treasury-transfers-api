using System.Net;

namespace Challenge.Domain.Exceptions;

public class FxRequiredException : DomainException
{
    public FxRequiredException()
        : base("FX rate is required and must be strictly positive when currencies differ.", "FX_REQUIRED", HttpStatusCode.BadRequest)
    {
    }
}
