using System.Net;

namespace RVM.TcgDex;

/// <summary>
/// The API answered with an error, or with something that is not the expected JSON.
/// </summary>
public class TcgDexException : Exception
{
    /// <summary>Creates the exception.</summary>
    public TcgDexException(string message, HttpStatusCode? statusCode = null, Uri? requestUri = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        RequestUri = requestUri;
    }

    /// <summary>HTTP status returned by the API; <c>null</c> when the failure was reading the body.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>The URL that failed.</summary>
    public Uri? RequestUri { get; }
}

/// <summary>
/// The requested resource does not exist. Methods named <c>GetAsync</c> return <c>null</c> instead
/// of throwing, like the official SDKs; navigation methods (<c>GetSetAsync</c>, <c>GetFullAsync</c>…)
/// throw, since the API itself pointed at the resource.
/// </summary>
public sealed class TcgDexNotFoundException : TcgDexException
{
    /// <summary>Creates the exception.</summary>
    public TcgDexNotFoundException(string message, Uri? requestUri = null)
        : base(message, HttpStatusCode.NotFound, requestUri)
    {
    }
}
