using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Espionage.LLM
{
    /// <summary>Non-blocking JSON POST over UnityWebRequest. Must be started from the Unity main thread.</summary>
    internal sealed class UnityWebRequestTransport : IHttpTransport
    {
        public async Task<HttpTransportResult> PostJsonAsync(string url, string json, int timeoutSeconds, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return new HttpTransportResult(HttpOutcome.Cancelled, 0, null, "Cancelled before sending.");

            var stopwatch = Stopwatch.StartNew();
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)) { contentType = "application/json" };
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;

                var completion = new TaskCompletionSource<bool>();
                var operation = request.SendWebRequest();
                operation.completed += _ => completion.TrySetResult(true);

                using (cancellationToken.Register(() =>
                {
                    request.Abort();
                    completion.TrySetResult(true);
                }))
                {
                    await completion.Task;
                }

                if (cancellationToken.IsCancellationRequested)
                    return new HttpTransportResult(HttpOutcome.Cancelled, 0, null, "Request cancelled.");

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        return new HttpTransportResult(
                            HttpOutcome.Completed, request.responseCode, request.downloadHandler?.text, request.error);

                    default:
                        bool timedOut = (request.error ?? string.Empty).IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                                        || stopwatch.Elapsed.TotalSeconds >= timeoutSeconds;
                        return new HttpTransportResult(
                            timedOut ? HttpOutcome.Timeout : HttpOutcome.ConnectionFailed, 0, null, request.error);
                }
            }
        }
    }
}
