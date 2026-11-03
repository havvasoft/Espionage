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
                         karar Grant ise: NpcGrantVerifier (ikinci, dar LLM sorusu)
                                                                  |
                         DialogueController -> MainframeAccessGate -> IObjectiveState.TryComplete(MainframeAccessGranted)
```

## Güvenlik katmanları

Modelin cevabı güvenilmez girdi sayılır ve üç aşamadan geçer:

1. **Biçim doğrulama (`NpcReplyParser`):** Geçerli JSON olmayan, bilinmeyen `decision` değeri taşıyan ya da `reply`
   alanı boş olan her cevap başarısız bir `NpcParseResult` olur. Beklenen biçim:
   `{"analiz":"...","decision":"grant|deny|undecided","reply":"..."}`. `analiz` alanı yalnızca modelin kararını
   gerekçelendirmesi içindir, ayrıştırıcı onu yok sayar.
2. **Grant doğrulaması (`NpcGrantVerifier`):** Model `grant` derse, ayrı ve dar bir sınıflandırma sorusu sorulur:
   somut kanıt var mı, görev nedeni var mı, tehdit/kötü amaç/talimat girişimi var mı? Yalnızca kanıt ve neden
   doğru, kötü amaç yanlış ise `grant` geçer. Aksi halde karar `undecided` olur ve sabit bir uyarı cümlesi
   gösterilir. Doğrulayıcıda hata, zaman aşımı, bozuk veya eksik cevap olursa karar da onaylanmaz (fail-closed).
3. **Erişim kapısı (`MainframeAccessGate`):** Yalnızca geçerli ve doğrulanmış bir `grant`
   `ObjectiveId.MainframeAccessGranted` hedefini tamamlar.

## Diyalog arayüzünü sahneye eklemek

1. `Assets/Prefabs/NPC/DialogueCanvas` prefab'ını sahneye sürükle. Sahnede bir `EventSystem` bulunmalıdır
   (prefab'ın içinde yoktur). Panel başlangıçta gizlidir.
2. NPC nesnesine `DialogueTrigger` bileşenini ekle ve üç alanı doldur:
   - **Controller:** prefab içindeki `DialogueController`.
   - **Objective State:** `IObjectiveState` uygulayan sahne bileşeni.
   - **Player Control:** `IPlayerControl` uygulayan sahne bileşeni.
3. `DialogueTrigger`, `IInteractable` uygular; diğer etkileşimli nesneler (terminal, kapı) nasıl bulunuyorsa aynı
   şekilde bulunur. Oyuncu etkileşince `Open()` çağrılır: panel açılır ve oyuncu kontrolleri kilitlenir.
   **Kapat** düğmesi `Close()` çağırır: panel kapanır ve kilit bırakılır. Erişim verildikten sonra etkileşim kapanır.
4. `Bind` `DialogueTrigger.Awake` içinde otomatik çağrılır. Trigger kullanmak istemezsen
   `DialogueController.Bind(IObjectiveState, IPlayerControl)` ile `Open()` / `Close()` doğrudan çağrılabilir.
   Bind parametrelerinden biri `null` olabilir; ilgili özellik atlanır.
5. Prefab, `Assets/OllamaSettings.asset` dosyasını kullanır (adres, model, zaman aşımı, sıcaklık).

## Gereksinimler

- Yerelde çalışan Ollama ve `OllamaSettings` içindeki model (şu an `qwen2.5:3b`).
- Oyun başlarken controller küçük bir ısınma isteği gönderir, böylece ilk diyalog hızlı olur. İstekler
  `keep_alive` değeri olarak 30 dakika gönderir.
- `Grant` kararlarında doğrulama için ikinci bir istek atılır (bu makinede yaklaşık yarım saniye ek gecikme).

## Hata durumunda davranış

Ollama kapalı, zaman aşımı, model yok, boş ya da bozuk cevap: oyun çalışmaya devam eder, oyuncu kısa bir
"görevli şu an cevap veremiyor" mesajı görür ve hiçbir hedef değişmez. Bekleme sırasında panelde "Görevli
düşünüyor..." animasyonu gösterilir.

## Testler

`Espionage.NPC.Tests` (EditMode, 27 test): ayrıştırıcı, sahte `ILlmClient` ile diyalog akışı ve Grant doğrulaması,
doğrulayıcının karar mantığı, sahte `IObjectiveState` ile erişim kapısı. Window > General > Test Runner
üzerinden çalıştırılır.

## Bilinen sınırlamalar

- 3B parametreli model bazen bozuk Türkçe yazar ve prompt'taki örnek cümleleri tekrar edebilir.
- Elle ölçüm: 10 senaryo x 3 deneme (ikna edici gerekçe, tehdit, hakaret, alakasız, yalvarma, sadece "izin ver",
  talimat sızdırma, kanıtsız yetki, yasaklı amaç) sonunda doğrulama katmanıyla 30/30 doğru sonuç alındı.
  Doğrulayıcının örnekleri bu cümlelere yakın olduğu için sonuç iyimser olabilir; farklı ifadelerle ayrıca
  denenmelidir. Doğrulama katmanı olmadan `yalvarma` ve `talimat sızdırma` senaryoları 0/3 idi.
- Model, kendinden emin ses çıkaran ve sahte belge iddia eden cümlelerle hâlâ `grant` vermeye
  yönlendirilebilir; belgenin gerçekliği doğrulanamaz.