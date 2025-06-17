using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Oyun arayüzündeki tüm panel, metin ve ses efektlerini yönetir.
/// Seviye bilgisi, zamanlayıcı, yön uyarıları, kelime ilerleme noktaları gibi öğeleri kontrol eder.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Yön Uyarı Paneli")]
    [Tooltip("Yön engelleme uyarı paneli.")]
    public GameObject directionWarningPanel;
    [Tooltip("Uyarı mesajını gösteren TMP bileşeni.")]
    public TextMeshProUGUI warningText;

    [Header("Seviye ve Hedef Kelime")]
    [Tooltip("Seviye numarasını gösteren TMP bileşeni.")]
    public TMP_Text levelText;
    [Tooltip("Hedef kelimeyi gösteren TMP bileşeni.")]
    public TMP_Text targetWordText;

    [Header("Zamanlayıcı")]
    [Tooltip("Zamanlayıcı metni.")]
    [SerializeField] private GameObject timerPanel;
    [SerializeField] private TMP_Text timerText;

    [Header("Timer Hide Delay")]
    [Tooltip("Timer paneli kapatılmadan önce bekleme süresi (saniye)")]
    [SerializeField] private float timerHideDelay = 0.5f;
    private Coroutine hideTimerCoroutine;

    private float timeRemaining = 60f;
    private bool timerRunning = false;

    [Header("Timer Pulse")]
    [Tooltip("10 saniye kala timer text için pulse süresi")]
    [SerializeField] private float timerPulseDuration = 0.5f;
    [Tooltip("Timer text en fazla bu oranda büyür")]
    [SerializeField] private float timerPulseScale = 1.2f;
    private Coroutine timerPulseCoroutine;

    [Header("Ses Efektleri")]
    [Tooltip("Ses kaynak bileşeni.")]
    public AudioSource audioSource;
    [Tooltip("Kelime tamamlandığında çalacak ses klibi.")]
    public AudioClip wordSuccessClip;
    [Tooltip("Kelime bittiğinde Victory panel'da çalınacak")]
    public AudioClip victoryClip;
    [Tooltip("Defeat panel'da çalınacak")]
    public AudioClip defeatClip;
    [Tooltip("Zaman 10 saniyeye gelince loop’la çalacak")]
    public AudioClip tickClip;
    [Tooltip("Süre bittiğinde çalınacak")]
    public AudioClip timeEndClip;
    [Tooltip("Hücre tıklanırken çalınacak ses klibi")]
    public AudioClip cellClickClip;

    private AudioSource tickSource;
    private bool tickPlaying = false;

    [Header("Kelime Noktaları")]
    [Tooltip("Dot’ları tutan container (Horizontal Layout Group)")]
    [SerializeField] private RectTransform dotContainer;
    [Tooltip("Normal dot arka plan imajları (3 adet)")]
    [SerializeField] private List<Image> dotBackgrounds;
    [Tooltip("Normal dot dolgu imajları (3 adet)")]
    [SerializeField] private List<Image> dotFills;
    [SerializeField] private float fillDuration = 0.5f;

    [Tooltip("Tutorial’da tek gösterilecek ek dot prefab’i")]
    [SerializeField] private GameObject tutorialDotPrefab;

    private GameObject tutorialDotInstance;

    private Vector2[] originalBgPositions;
    private Vector2[] originalFillPositions;

    /// <summary>
    /// Singleton düzenlemesini sağlar ve çoğaltma varsa yok eder.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        tickSource = gameObject.AddComponent<AudioSource>();
        tickSource.clip = tickClip;
        tickSource.loop = true;
        tickSource.playOnAwake = false;

        timerPanel.SetActive(false);
        directionWarningPanel.SetActive(false);

        int nBg = dotBackgrounds.Count;
        originalBgPositions = new Vector2[nBg];
        for (int i = 0; i < nBg; i++)
            originalBgPositions[i] = dotBackgrounds[i].rectTransform.anchoredPosition;

        int nFill = dotFills.Count;
        originalFillPositions = new Vector2[nFill];
        for (int i = 0; i < nFill; i++)
            originalFillPositions[i] = dotFills[i].rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Zamanlayıcıyı günceller ve süresi bitince oyun yöneticisine bildirir.
    /// </summary>
    private void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;

            if (tickPlaying) { tickSource.Stop(); tickPlaying = false; }

            if (timerPulseCoroutine != null) StopCoroutine(timerPulseCoroutine);
            timerText.rectTransform.localScale = Vector3.one;
            timerPulseCoroutine = null;

            if (audioSource && timeEndClip != null)
                audioSource.PlayOneShot(timeEndClip);

            SynapseGameManager.Instance.OnTimeOver();
        }
        else
        {
            if (timeRemaining <= 10f && !tickPlaying)
            {
                tickSource.Play();
                tickPlaying = true;
                if (timerPulseCoroutine != null) StopCoroutine(timerPulseCoroutine);
                timerPulseCoroutine = StartCoroutine(PulseTimerText());
            }
        }

        timerText.text = FormatTime(timeRemaining);
    }

    /// <summary>
    /// Seviye ve hedef kelime bilgisini günceller.
    /// Yeni seviyenin ilk kelimesiyse kelime noktalarını başlatır.
    /// </summary>
    /// <param name="levelIndex">Seviye sıfır tabanlı indeksi.</param>
    /// <param name="word">O anki hedef kelime.</param>
    public void UpdateLevelInfo(int levelIndex, string word)
    {
        UpdateLevel(levelIndex + 1);
        UpdateTargetWord(word);

        int count = LevelManager.Instance.allLevels[levelIndex].targetWords.Count;
        if (LevelManager.Instance.currentWordIndex == 0)
            InitWordDots(dotFills.Count);
    }

    /// <summary>
    /// Seviye metnini günceller.
    /// </summary>
    /// <param name="level">Gösterilecek seviye numarası.</param>
    public void UpdateLevel(int level)
    {
        levelText.text = $" {level}";
    }

    /// <summary>
    /// Hedef kelime metnini günceller.
    /// </summary>
    /// <param name="word">Yeni hedef kelime.</param>
    public void UpdateTargetWord(string word)
    {
        targetWordText.text = word.ToUpper();
    }

    /// <summary>
    /// Zamanlayıcıyı verilen saniye ile başlatır.
    /// </summary>
    /// <param name="seconds">Başlangıç süresi (saniye).</param>
    public void StartTimer(float seconds)
    {
        timeRemaining = seconds;
        timerRunning = true;
        timerPanel.SetActive(true);
        if (tickPlaying) { tickSource.Stop(); tickPlaying = false; }
        if (timerPulseCoroutine != null) StopCoroutine(timerPulseCoroutine);
        timerText.rectTransform.localScale = Vector3.one;
        timerPulseCoroutine = null;
        timerText.text = FormatTime(timeRemaining);
    }

    /// <summary>
    /// Zamanlayıcı metninin görünürlüğünü ayarlar.
    /// </summary>
    /// <param name="active">Aktif/pasif durumu.</param>
    public void SetTimerActive(bool active)
    {
        if (active)
        {
            if (hideTimerCoroutine != null)
                StopCoroutine(hideTimerCoroutine);
            timerPanel.SetActive(true);
        }
        else
        {
            if (hideTimerCoroutine != null)
                StopCoroutine(hideTimerCoroutine);
            hideTimerCoroutine = StartCoroutine(HideTimerDelayed());
        }
    }

    private IEnumerator HideTimerDelayed()
    {
        yield return new WaitForSeconds(timerHideDelay);
        timerPanel.SetActive(false);
        hideTimerCoroutine = null;
    }

    /// <summary>
    /// Süreyi MM:SS formatında döndürür.
    /// </summary>
    /// <param name="t">Kalan süre (saniye).</param>
    /// <returns>Formatlanmış süre metni.</returns>
    private string FormatTime(float t)
    {
        int min = Mathf.FloorToInt(t / 60f);
        int sec = Mathf.FloorToInt(t % 60f);
        return $"{min:00}:{sec:00}";
    }

    /// <summary>
    /// Bir yönde geçiş engellenirse uyarı mesajı gösterir.
    /// </summary>
    public void ShowDirectionWarning()
    {
        StopAllCoroutines();
        StartCoroutine(ShowWarningRoutine("No passage in this direction!"));
    }

    /// <summary>
    /// Uyarı panelini ölçek animasyonuyla gösterip belirli süre sonra gizler.
    /// </summary>
    /// <param name="message">Gösterilecek uyarı metni.</param>
    private IEnumerator ShowWarningRoutine(string message)
    {
        warningText.text = message;
        directionWarningPanel.transform.localScale = Vector3.zero;
        directionWarningPanel.SetActive(true);

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(0f, 0.3f, t / 0.3f);
            directionWarningPanel.transform.localScale = new Vector3(s, s, s);
            yield return null;
        }

        yield return new WaitForSeconds(2f);
        directionWarningPanel.SetActive(false);
    }

    /// <summary>
    /// Kamera sarsıntısı animasyonu başlatır.
    /// </summary>
    public void ShakeCamera()
    {
        StartCoroutine(ShakeRoutine());
    }

    /// <summary>
    /// Belirli süre ve genlikte kamera pozisyonunu rastgele değiştirerek sarsar.
    /// </summary>
    private IEnumerator ShakeRoutine()
    {
        var cam = Camera.main.transform;
        var originalPos = cam.position;
        float duration = 0.3f, magnitude = 0.2f, elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cam.position = originalPos + new Vector3(x, y, 0);
            yield return null;
        }

        cam.position = originalPos;
    }

    /// <summary>
    /// Zamanlayıcıyı durdurur.
    /// </summary>
    public void StopTimer()
    {
        timerRunning = false;
        SetTimerActive(false);  // artık delay’li kapanacak
    }

    /// <summary>
    /// Kelime ilerleme noktalarını başlangıç durumuna ayarlar.
    /// </summary>
    /// <param name="count">Hedef kelime uzunluğu (nokta sayısı).</param>
    public void InitWordDots(int count)
    {
        if (tutorialDotInstance != null)
        {
            Destroy(tutorialDotInstance);
            tutorialDotInstance = null;
        }

        //    dotBackgrounds[i].transform.parent == Dot_i GameObject
        foreach (var bg in dotBackgrounds)
            bg.transform.parent.gameObject.SetActive(false);

        bool isTutorial = LevelManager.Instance.currentLevelIndex == 0
                       && LevelManager.Instance.currentWordIndex == 0;

        if (isTutorial)
        {
            foreach (var f in dotFills)
                f.transform.parent.gameObject.SetActive(false);

            tutorialDotInstance = Instantiate(
                tutorialDotPrefab,
                dotContainer,
                false
            );
            tutorialDotInstance.transform.SetAsLastSibling();

            dotContainer.anchoredPosition = Vector2.zero;
        }
        else
        {
            for (int i = 0; i < dotBackgrounds.Count; i++)
            {
                if (i < count)
                {
                    var parentGO = dotBackgrounds[i].transform.parent.gameObject;
                    parentGO.SetActive(true);

                    dotBackgrounds[i].gameObject.SetActive(true);

                    var fillImg = dotFills[i];
                    fillImg.type = Image.Type.Filled;
                    fillImg.fillMethod = Image.FillMethod.Radial360;
                    fillImg.fillAmount = 0f;
                    fillImg.gameObject.SetActive(false);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(dotContainer);
            dotContainer.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// Belirtilen indeksdeki kelime noktasını dolu olarak işaretler.
    /// </summary>
    /// <param name="index">Doldurulacak nokta indeksi.</param>
    public void MarkWordComplete(int index)
    {
        bool isTutorial = LevelManager.Instance.currentLevelIndex == 0
                       && LevelManager.Instance.currentWordIndex == 0;
        if (isTutorial) return;

        if (index < 0 || index >= dotFills.Count) return;

        var img = dotFills[index];
        img.fillAmount = 0f;
        img.gameObject.SetActive(true);
        StartCoroutine(AnimateDotFill(img));
    }

    /// <summary>
    /// Kayıttan hızlı yüklemede anında doldurur.
    /// </summary>
    public void SetWordCompleteInstant(int index)
    {
        bool isTutorial = LevelManager.Instance.currentLevelIndex == 0
                       && LevelManager.Instance.currentWordIndex == 0;
        if (isTutorial) return;

        if (index < 0 || index >= dotFills.Count) return;
        var img = dotFills[index];
        img.gameObject.SetActive(true);
        img.fillAmount = 1f;
    }

    /// <summary>
    /// Nokta dolum animasyonunu gerçekleştirir.
    /// </summary>
    /// <param name="img">Animasyonu uygulanacak Image bileşeni.</param>
    private IEnumerator AnimateDotFill(Image img)
    {
        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            img.fillAmount = Mathf.Lerp(0f, 1f, elapsed / fillDuration);
            yield return null;
        }
        img.fillAmount = 1f;
    }

    private IEnumerator PulseTimerText()
    {
        var rt = timerText.rectTransform;
        Vector3 baseScale = rt.localScale;
        Vector3 maxScale = baseScale * timerPulseScale;
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime;
            float f = Mathf.PingPong(t / timerPulseDuration, 1f);
            rt.localScale = Vector3.Lerp(baseScale, maxScale, f);
            yield return null;
        }
    }

    /// <summary>Hücre tıklama sesini çalar.</summary>
    public void PlayCellClick()
    {
        if (audioSource != null && cellClickClip != null)
            audioSource.PlayOneShot(cellClickClip);
    }

    public void PlaySuccessSound()
    {
        if (audioSource != null && wordSuccessClip != null)
            audioSource.PlayOneShot(wordSuccessClip);
    }

    public void PlayVictorySound()
    {
        if (audioSource != null && victoryClip != null)
            audioSource.PlayOneShot(victoryClip);
    }

    public void PlayDefeatSound()
    {
        if (audioSource != null && defeatClip != null)
            audioSource.PlayOneShot(defeatClip);
    }
}