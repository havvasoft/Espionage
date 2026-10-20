using System.Threading;
using System.Threading.Tasks;

namespace Espionage.LLM
{
    internal enum HttpOutcome
    {
        /// <summary>An HTTP response was received (any status code).</summary>
        Completed,
        ConnectionFailed,
        Timeout,
        Cancelled
    }

    internal readonly struct HttpTransportResult
    {
        public HttpOutcome Outcome { get; }
        public long StatusCode { get; }
        public string Body { get; }
        public string Error { get; }

        public HttpTransportResult(HttpOutcome outcome, long statusCode, string body, string error)
        {
            Outcome = outcome;
            StatusCode = statusCode;
            Body = body;
            Error = error;
        }
    }

    internal interface IHttpTransport
    {
        Task<HttpTransportResult> PostJsonAsync(string url, string json, int timeoutSeconds, CancellationToken cancellationToken);
    }
}
