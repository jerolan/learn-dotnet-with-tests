using System.Net;

namespace TaskTracker.Client;

/// <summary>The API returned a response outside the success range.</summary>
public sealed class TaskClientException : Exception
{
    /// <summary>Creates the exception from the response status and body.</summary>
    /// <param name="statusCode">The status returned by the API.</param>
    /// <param name="message">The status and response body.</param>
    public TaskClientException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>The status returned by the API.</summary>
    public HttpStatusCode StatusCode { get; }
}
