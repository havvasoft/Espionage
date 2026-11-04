# Espionage.LLM

Local-only LLM infrastructure (Ollama). No cloud APIs. References `Espionage.Core` only.

```
Abstractions/   ILlmClient, LlmRequest, LlmMessage, LlmRole, LlmResult, LlmErrorKind
Configuration/  OllamaConfig (validated), OllamaSettings (ScriptableObject)
Ollama/         OllamaClient, OllamaResponseParser, IHttpTransport, UnityWebRequestTransport, internal DTOs
```

## Usage

```csharp
var client = settings.CreateClient();            // OllamaSettings asset, or: new OllamaClient(new OllamaConfig())
var result = await client.SendAsync(
    LlmRequest.FromPrompts("You are a mainframe guard.", "Let me in.", expectJson: true),
    destroyCancellationToken);

if (!result.Success) { /* result.ErrorKind / ErrorMessage; game keeps running */ return; }
string untrustedText = result.Content;            // validate before touching game state
```

Call from the Unity main thread. `SendAsync` never throws; failures are `LlmResult` values.

## Configuration

Create an asset: *Assets > Create > Espionage > LLM > Ollama Settings* (default URL `http://localhost:11434`,
model `llama3.2`, 30 s timeout). Defaults live only in `OllamaConfig`.

## Manual test with Ollama

1. `ollama serve` (or the desktop service) and `ollama pull llama3.2`.
2. `curl http://localhost:11434/api/tags` should list the model.
3. Call `SendAsync` from a temporary script in a scratch scene (do not edit shared scenes).

| Case | How to trigger | Expected `ErrorKind` |
|---|---|---|
| Success | normal call | `None` |
| Ollama down | stop the server | `Unavailable` |
| Wrong model | set model to `does-not-exist` | `ModelNotFound` |
| Timeout | `timeoutSeconds = 1` with a big model/cold start | `Timeout` |
| Cancelled | cancel the token mid-call | `Cancelled` |
| Bad config | URL `foo`, empty model | `InvalidConfiguration` |
| Empty request | no/blank messages | `InvalidRequest` |
| Empty/malformed body | point URL at a stub server returning `""` / `not json` | `EmptyResponse` / `MalformedResponse` |

Persuasive / angry / irrelevant prompt tests belong to the NPC layer and use this client.

## Unit testing

`IHttpTransport` and `OllamaResponseParser` are internal and exposed to `Espionage.LLM.Tests`
(`AssemblyInfo.cs`), so parser and client logic can be tested with a fake transport, no server needed.
