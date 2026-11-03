# Espionage.NPC ve Espionage.UI

Oyuncunun ana bilgisayar odasına girip giremeyeceğine karar veren yerel LLM tabanlı NPC.

```
Espionage.UI  ->  Espionage.NPC  ->  Espionage.LLM  ->  Espionage.Core
```

NPC tarafının Companion veya Guards sınıflarına bağımlılığı yoktur. Ortak durum yalnızca `Espionage.Core`
arayüzleri üzerinden paylaşılır.

## Akış

```
Oyuncu metni -> NpcPromptBuilder -> ILlmClient (Ollama) -> NpcReplyParser -> NpcParseResult
                                                                    |
                                       MainframeAccessGate -> IObjectiveState.TryComplete(MainframeAccessGranted)
```

Modelden şu biçimde bir cevap beklenir: `{"decision":"grant|deny|undecided","reply":"..."}`. Gelen metin
güvenilmez girdi sayılır: geçerli JSON olmayan, bilinmeyen bir karar içeren ya da `reply` alanı boş olan her cevap
başarısız bir `NpcParseResult` olur. Yalnızca geçerli bir `grant` kararı `ObjectiveId.MainframeAccessGranted`
hedefini tamamlar.

## Diyalog arayüzünü kullanmak

1. `Assets/Prefabs/NPC/DialogueCanvas` prefab'ını sahneye sürükle. Sahnede bir `EventSystem` bulunmalıdır
   (prefab'ın içinde yoktur).
2. `DialogueController.Bind(IObjectiveState, IPlayerControl)` metodunu bir kez çağır. İki parametre de `null`
   olabilir; `null` verilen özellik atlanır.
3. Oyuncu konuşmaya başlarken `Open()` (paneli gösterir, oyuncu kontrollerini kilitler), bitince `Close()` çağır.
4. Prefab, `Assets/OllamaSettings.asset` dosyasını kullanır (adres, model, zaman aşımı, sıcaklık).

## Gereksinimler

- Yerelde çalışan Ollama ve `OllamaSettings` içindeki model (şu an `qwen2.5:3b`).
- Oyun başlarken controller küçük bir ısınma isteği gönderir, böylece ilk diyalog hızlı olur. İstekler
  `keep_alive` değeri olarak 30 dakika gönderir.

## Hata durumunda davranış

Ollama kapalı, zaman aşımı, model yok, boş ya da bozuk cevap: oyun çalışmaya devam eder, oyuncu kısa bir
"görevli şu an cevap veremiyor" mesajı görür ve hiçbir hedef değişmez.

## Testler

`Espionage.NPC.Tests` (EditMode, 18 test): ayrıştırıcı, sahte `ILlmClient` ile diyalog akışı, sahte
`IObjectiveState` ile erişim kapısı. Window > General > Test Runner üzerinden çalıştırılır.

## Bilinen sınırlamalar

- 3B parametreli model bazen bozuk Türkçe yazar ve prompt'taki örnek cümleleri tekrar edebilir.
- Kararlar küçük bir cümle kümesiyle elle denendi (ikna edici, tehdit, hakaret, alakasız, düz rica). Model, kendinden
  emin ses çıkaran iddialarla hâlâ `grant` vermeye ikna edilebilir.