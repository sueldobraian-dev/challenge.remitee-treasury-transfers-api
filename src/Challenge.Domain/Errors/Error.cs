using System.Collections.Generic;
using System.Net;

namespace Challenge.Domain.Errors;

public record Error(HttpStatusCode HttpStatusCode, string Code, string Message, string Description, IEnumerable<ErrorDetail>? Errors = null)
{
    public static readonly Error None = new((HttpStatusCode)0, string.Empty, string.Empty, string.Empty);
}
