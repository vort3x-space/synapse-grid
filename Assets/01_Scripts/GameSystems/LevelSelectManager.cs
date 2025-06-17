using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelSelectManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject levelSelectPanel;
    public Transform contentParent;
    public GameObject levelButtonPrefab;

    void Awake()
    {
        levelSelectPanel.SetActive(false);
    }

    public void ShowLevelSelect()
    {
        levelSelectPanel.SetActive(true);
        PopulateButtons();
    }

    public void HideLevelSelect()
    {
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);
        levelSelectPanel.SetActive(false);
    }

    private void PopulateButtons()
    {
        var allLvls = LevelManager.Instance.allLevels;
        int unlocked = PlayerPrefs.GetInt("SavedLevelIndex", 0);

        for (int i = 0; i < allLvls.Count; i++)
        {
            var go = Instantiate(levelButtonPrefab, contentParent);
            var txt = go.GetComponentInChildren<TMP_Text>();
            txt.text = $"LEVEL {i + 1}";

            var btn = go.GetComponent<Button>();
            int idx = i;
            btn.onClick.AddListener(() => OnLevelButton(idx));

            // kilitli / kilitsiz görünümler:
            // btn.interactable = (i <= unlocked);
            btn.interactable = true;
        }
    }

    private void OnLevelButton(int levelIndex)
    {
        LevelManager.Instance.LoadLevel(levelIndex, 0);

        levelSelectPanel.SetActive(false);
    }
}
