using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Main Screen")]
    [Tooltip("Açılışta görünen ana menü paneli")]
    [SerializeField] private GameObject mainScreenPanel;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;
    [Tooltip("Loading süresi (saniye)")]
    [SerializeField] private float loadingDuration = 2f;

    [Header("Panels")]
    [Tooltip("Settings panelinin RectTransform'u")]
    [SerializeField] private RectTransform settingsPanel;
    [Tooltip("Ayarlar paneli açılış/kapanış animasyon süresi (saniye)")]
    [SerializeField] private float settingsAnimDuration = 0.25f;
    [Header("Panels")]
    [SerializeField] private RectTransform victoryPanelRt;
    [SerializeField] private CanvasGroup victoryCanvasGroup;
    [SerializeField] private RectTransform defeatPanelRt;
    [SerializeField] private CanvasGroup defeatCanvasGroup;

    [Header("Animation Settings")]
    [Tooltip("Panel slide-in süresi (s)")]
    [SerializeField] private float slideDuration = 0.5f;
    [Tooltip("Panel’in ne kadar yukarı/aşağıdan gelmesi")]
    [SerializeField] private float slideOffset = 200f;

    [Header("Toggles & Icons")]
    [Tooltip("Sound efekti toggle butonu")]
    [SerializeField] private Button soundButton;
    [Tooltip("Sound ON ikonu (GameObject)")]
    [SerializeField] private GameObject soundOnIcon;
    [Tooltip("Sound OFF ikonu (GameObject)")]
    [SerializeField] private GameObject soundOffIcon;

    [Tooltip("Music toggle butonu")]
    [SerializeField] private Button musicButton;
    [SerializeField] private GameObject musicOnIcon;
    [SerializeField] private GameObject musicOffIcon;

    [Tooltip("Vibration toggle butonu")]
    [SerializeField] private Button vibrationButton;
    [SerializeField] private GameObject vibrationOnIcon;
    [SerializeField] private GameObject vibrationOffIcon;

    [Header("Audio Sources")]
    [Tooltip("Sound effectleri oynatan AudioSource")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("Arkaplan müziği oynatan AudioSource")]
    [SerializeField] private AudioSource musicSource;

    private bool soundEnabled;
    private bool musicEnabled;
    private bool vibrationEnabled;

    private const string PREF_SOUND = "SoundEnabled";
    private const string PREF_MUSIC = "MusicEnabled";
    private const string PREF_VIBRATION = "VibrationEnabled";

    private bool isSettingsOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        loadingPanel.SetActive(true);
        mainScreenPanel.SetActive(false);

        soundEnabled = PlayerPrefs.GetInt(PREF_SOUND, 1) == 1;
        musicEnabled = PlayerPrefs.GetInt(PREF_MUSIC, 1) == 1;
        vibrationEnabled = PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;

        soundButton.onClick.AddListener(ToggleSound);
        musicButton.onClick.AddListener(ToggleMusic);
        vibrationButton.onClick.AddListener(ToggleVibration);

        UpdateSoundUI();
        UpdateMusicUI();
        UpdateVibrationUI();

        settingsPanel.localScale = Vector3.zero;
        settingsPanel.gameObject.SetActive(false);
    }

    private void Start()
    {
        Time.timeScale = 0f;
        StartCoroutine(DoLoading());
        victoryPanelRt.GetComponentInChildren<VideoPlayer>().Prepare();
    }

    private IEnumerator DoLoading()
    {
        for (int i = 1; i <= 100; i++)
        {
            loadingText.text = $"Loading {i}%";
            yield return new WaitForSecondsRealtime(loadingDuration / 100f);
        }

        loadingPanel.SetActive(false);

        mainScreenPanel.SetActive(true);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Play butonuna bağlayın: Kaydı yükle ve oyunu başlat.
    /// </summary>
    public void OnPlayButton()
    {
        mainScreenPanel.SetActive(false);
        Time.timeScale = 1f;
        LevelManager.Instance.LoadSavedProgress();
        SynapseGameManager.Instance.lineDrawer.ShowLine();

        var lvl = LevelManager.Instance;
        if (lvl.currentLevelIndex == 0 && lvl.currentWordIndex == 0)
            TutorialManager.Instance.StartTutorial();

        UpdateGridControls();
    }

    #region Settings Panel
    public void OpenSettings()
    {
        if (isSettingsOpen) return;
        isSettingsOpen = true;
        settingsPanel.gameObject.SetActive(true);
        StartCoroutine(ScaleRect(settingsPanel, Vector3.zero, Vector3.one, settingsAnimDuration));
    }

    public void CloseSettings()
    {
        if (!isSettingsOpen) return;
        isSettingsOpen = false;
        StartCoroutine(ScaleRect(settingsPanel, Vector3.one, Vector3.zero, settingsAnimDuration, () =>
        {
            settingsPanel.gameObject.SetActive(false);
        }));
    }

    public void ShowMainMenu()
    {
        ResetWinVideo();
        mainScreenPanel.SetActive(true);
    }

    private IEnumerator ScaleRect(RectTransform rt, Vector3 from, Vector3 to, float duration, Action onComplete = null)
    {
        float elapsed = 0f;
        rt.localScale = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rt.localScale = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        rt.localScale = to;
        onComplete?.Invoke();
    }
    #endregion

    #region Sound / Music / Vibration
    private void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        PlayerPrefs.SetInt(PREF_SOUND, soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        UpdateSoundUI();
    }

    private void ToggleMusic()
    {
        musicEnabled = !musicEnabled;
        PlayerPrefs.SetInt(PREF_MUSIC, musicEnabled ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMusicUI();
    }

    private void ToggleVibration()
    {
        vibrationEnabled = !vibrationEnabled;
        PlayerPrefs.SetInt(PREF_VIBRATION, vibrationEnabled ? 1 : 0);
        PlayerPrefs.Save();
        UpdateVibrationUI();
    }

    private void UpdateSoundUI()
    {
        soundOnIcon.SetActive(soundEnabled);
        soundOffIcon.SetActive(!soundEnabled);
        if (sfxSource) sfxSource.mute = !soundEnabled;
    }

    private void UpdateMusicUI()
    {
        musicOnIcon.SetActive(musicEnabled);
        musicOffIcon.SetActive(!musicEnabled);
        if (musicSource) musicSource.mute = !musicEnabled;
    }

    private void UpdateVibrationUI()
    {
        vibrationOnIcon.SetActive(vibrationEnabled);
        vibrationOffIcon.SetActive(!vibrationEnabled);
    }

    /// <summary>
    /// Diğer scriptlerde kullanmak için:
    /// if(GameUIManager.Instance.VibrationEnabled) Handheld.Vibrate();
    /// </summary>
    public bool VibrationEnabled => vibrationEnabled;
    #endregion

    #region Defeat / Victory Panels

    public void ShowDefeat()
    {
        SynapseGameManager.Instance.ResetSelection();
        SynapseGameManager.Instance.lineDrawer.HideLine();
        UIManager.Instance.PlayDefeatSound();
        defeatPanelRt.GetComponent<RectTransform>().localScale = Vector3.one;
        defeatPanelRt.GetComponentInChildren<VideoPlayer>().Play();
        StartCoroutine(SlideInPanel(defeatPanelRt, defeatCanvasGroup, fromAbove: false));
    }

    public void ShowVictory()
    {
        SynapseGameManager.Instance.ResetSelection();
        SynapseGameManager.Instance.lineDrawer.HideLine();
        UIManager.Instance.PlayVictorySound();
        victoryPanelRt.GetComponent<RectTransform>().localScale = Vector3.one;
        victoryPanelRt.GetComponentInChildren<VideoPlayer>().Play();
        StartCoroutine(SlideInPanel(victoryPanelRt, victoryCanvasGroup, fromAbove: true));
    }

    public void OnNextButton()
    {
        SynapseGameManager.Instance.lineDrawer.ShowLine();
        SynapseGameManager.Instance.LoadNextLevel();
        ResetWinVideo();

        UpdateGridControls();
    }

    public void OnRestartButton()
    {
        SynapseGameManager.Instance.lineDrawer.ShowLine();
        SynapseGameManager.Instance.ResetSelection();
        SynapseGameManager.Instance.inputLocked = false;
        LevelManager.Instance.LoadLevel(
            LevelManager.Instance.currentLevelIndex,
            LevelManager.Instance.currentWordIndex
        );
        ResetWinVideo();

        UpdateGridControls();
    }

    #region Grid Control Buttons

    [Header("Grid Control Buttons")]
    [Tooltip("Grid yeniden başlatma butonu")]
    public GameObject restartButton;
    [Tooltip("Kelime ipucu butonu")]
    public GameObject hintButton;

    /// <summary>
    /// Butonları tutorial’da kapat, diğer level’larda aç.
    /// </summary>
    private void UpdateGridControls()
    {
        var lvl = LevelManager.Instance;
        bool isTutorial = lvl.currentLevelIndex == 0
                       && lvl.currentWordIndex == 0;

        ShowGridControls(!isTutorial);
    }

    public void ShowGridControls(bool show)
    {
        if (restartButton != null) restartButton.SetActive(show);
        if (hintButton != null) hintButton.SetActive(show);
    }

    #endregion


    private IEnumerator SlideInPanel(RectTransform rt, CanvasGroup cg, bool fromAbove)
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        Vector2 targetPos = rt.anchoredPosition;
        Vector2 startPos = targetPos + (fromAbove
            ? Vector2.up * slideOffset
            : Vector2.down * slideOffset);

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        rt.anchoredPosition = targetPos;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    void ResetWinVideo()
    {
        victoryPanelRt.GetComponent<RectTransform>().localScale = Vector3.zero;
        victoryPanelRt.GetComponentInChildren<VideoPlayer>().Stop();
        victoryPanelRt.GetComponentInChildren<VideoPlayer>().frame = 0;
        victoryPanelRt.GetComponentInChildren<VideoPlayer>().Prepare();

        defeatPanelRt.GetComponent<RectTransform>().localScale = Vector3.zero;
        defeatPanelRt.GetComponentInChildren<VideoPlayer>().Stop();
        defeatPanelRt.GetComponentInChildren<VideoPlayer>().frame = 0;
        defeatPanelRt.GetComponentInChildren<VideoPlayer>().Prepare();
    }
    #endregion
}
