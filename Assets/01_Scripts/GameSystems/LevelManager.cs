using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Seviye ve hedef kelime ilerleyişini yönetir ve basit bir Singleton deseni uygular.
/// </summary>
public class LevelManager : MonoBehaviour
{
    /// <summary>Global erişim için tek örnek.</summary>
    public static LevelManager Instance { get; private set; }

    [Tooltip("Oynanış sırasına göre tüm seviye ayarları.")]
    public List<LevelData> allLevels = new List<LevelData>();

    [Tooltip("Seviye ayarlarını uygulayan GridManager referansı.")]
    public GridManager gridManager;

    // --- PlayerPrefs anahtarları ---
    private const string PREF_LEVEL = "SavedLevelIndex";
    private const string PREF_WORD = "SavedWordIndex";

    /// <summary>Şu anda aktif olan seviye indeksi.</summary>
    public int currentLevelIndex { get; private set; }

    /// <summary>Şu anda aktif olan kelime indeksi.</summary>
    public int currentWordIndex { get; private set; }

    private bool tutorialPlayed = false;

    private void Awake()
    {
        // Singleton düzenlemesi: yalnızca bir örnek kalır.
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
        LoadSavedProgress();
    }

    public void LoadSavedProgress()
    {
        int lvl = PlayerPrefs.GetInt(PREF_LEVEL, 0);
        int word = PlayerPrefs.GetInt(PREF_WORD, 0);
        lvl = Mathf.Clamp(lvl, 0, allLevels.Count - 1);
        word = Mathf.Clamp(word, 0, allLevels[lvl].targetWords.Count - 1);
        LoadLevel(lvl, word);
    }

    /// <summary>
    /// Belirtilen seviye ve kelimeyi yükler, oyun ve UI durumunu günceller.
    /// </summary>
    /// <param name="levelIndex">Yüklenecek seviye indeksi.</param>
    /// <param name="wordIndex">O seviyedeki hedef kelime indeksi.</param>
    public void LoadLevel(int levelIndex, int wordIndex)
    {
        if (levelIndex < 0 || levelIndex >= allLevels.Count)
        {
            Debug.LogError($"LoadLevel hatası: seviye {levelIndex} aralık dışı.");
            return;
        }

        LevelData levelData = allLevels[levelIndex];

        if (wordIndex < 0 || wordIndex >= levelData.targetWords.Count)
        {
            Debug.LogError($"LoadLevel hatası: kelime {wordIndex} aralık dışı (Seviye: {levelData.levelName}).");
            return;
        }

        currentLevelIndex = levelIndex;
        currentWordIndex = wordIndex;
        string hedefKelime = levelData.targetWords[wordIndex];

        SynapseGameManager.Instance.targetWord = hedefKelime;

        UIManager.Instance.UpdateLevelInfo(levelIndex, hedefKelime);

        UIManager.Instance.InitWordDots(allLevels[levelIndex].targetWords.Count);

        for (int i = 0; i < wordIndex; i++)
            UIManager.Instance.SetWordCompleteInstant(i);

        UIManager.Instance.SetTimerActive(levelData.enableTimer);
        if (levelData.enableTimer)
            UIManager.Instance.StartTimer(levelData.timerDuration);
        else
            UIManager.Instance.StopTimer();

        gridManager.ApplyLevelSettings(levelData, hedefKelime);

        SaveProgress();

        SynapseGameManager.Instance.inputLocked = false;
        SynapseGameManager.Instance.lineDrawer.ShowLine();
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(PREF_LEVEL, currentLevelIndex);
        PlayerPrefs.SetInt(PREF_WORD, currentWordIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Mevcut seviyede bir sonraki kelimeyi veya sonraki seviyeyi yükler
    /// </summary>
    public void LoadNextLevel()
    {
        LevelData levelData = allLevels[currentLevelIndex];

        if (currentWordIndex + 1 < levelData.targetWords.Count)
        {
            LoadLevel(currentLevelIndex, currentWordIndex + 1);
            return;
        }

        int nextLevel = currentLevelIndex + 1;
        if (nextLevel < allLevels.Count)
        {
            LoadLevel(nextLevel, 0);
        }
        else
        {
            Debug.Log("All levels completed — returning to main menu.");
            GameUIManager.Instance.ShowMainMenu();
        }
    }
}