using System.Threading;
using System.Threading.Tasks;

namespace Espionage.LLM
{
    /// <summary>
    /// Provider-agnostic entry point for talking to a language model.
    /// Implementations must never throw for runtime failures; they report them through <see cref="LlmResult"/>.
    /// </summary>
    public interface ILlmClient
    {
        Task<LlmResult> SendAsync(LlmRequest request, CancellationToken cancellationToken = default);
    }
}
