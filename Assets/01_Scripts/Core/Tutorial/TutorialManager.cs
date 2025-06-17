using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Dependencies")]
    public GridManager gridManager;
    public SynapseGameManager gameManager;

    [Header("Audio")]
    public AudioSource tutorialAudioSource;
    public AudioClip tutorialClickClip;

    [Header("UI")]
    public GameObject tutorialPanel;
    public TMP_Text tutorialText;
    public Button nextButton;

    [Header("Finger Hint")]
    [Tooltip("Canvas üzerinde parmak gösterimi için Image")]
    public Image fingerImage;
    private bool inputLocked;

    private Coroutine pulseCoroutine;

    private struct Step { public Feature feature; public string description; }
    private enum Feature { Locked, Hidden, Danger, DirectionBlock, Swap, Timer }
    private List<Step> steps;
    private int currentStep = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        tutorialPanel.SetActive(false);
    }

    public void StartTutorial()
    {
        gameManager.inputLocked = true;
        tutorialPanel.SetActive(true);

        steps = new List<Step>()
        {
            new Step { feature = Feature.Locked,           description = "This cell is locked: cannot be selected." },
            new Step { feature = Feature.Hidden,           description = "This cell is hidden: click first to open it." },
            new Step { feature = Feature.Danger,           description = "This cell is dangerous: if you try to select it, the selection will be reset." },
            new Step { feature = Feature.DirectionBlock,   description = "This cell has a directional barrier: you cannot follow the arrow." },
            new Step { feature = Feature.Swap,             description = "Some cells are randomly swapped." },
            new Step { feature = Feature.Timer,            description = "Time is reduced in timed episodes." },
        };
        currentStep = 0;
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        tutorialText.text = steps[currentStep].description;

        UnhighlightAll();

        var cell = FindCellWithFeature(steps[currentStep].feature);
        if (cell != null)
        {
            cell.Highlight(Color.cyan);
            pulseCoroutine = StartCoroutine(PulseCell(cell.transform));
        }

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(() =>
        {
            tutorialAudioSource.PlayOneShot(tutorialClickClip);
            OnNext();
        });
    }

    private void OnNext()
    {
        UnhighlightAll();

        currentStep++;
        if (currentStep < steps.Count)
        {
            ShowCurrentStep();
        }
        else
        {
            EndTutorial();
        }
    }

    private void EndTutorial()
    {
        UnhighlightAll();
        tutorialPanel.SetActive(false);


        var lvl = LevelManager.Instance;
        lvl.LoadLevel(lvl.currentLevelIndex, lvl.currentWordIndex);

        foreach (var cell in gridManager.GetComponentsInChildren<Cell>())
            cell.GetComponent<Button>().interactable = true;

        StartCoroutine(ShowConnectDemo());

    }

    /// <summary>
    /// Hedef kelimenin ilk iki hücresi arasında parmakla dokunma ve sürükleme animasyonu.
    /// </summary>
    private IEnumerator ShowConnectDemo()
    {
        var coords = gridManager.targetWordCoords;
        if (coords.Count < 2)
        {
            UnlockInput();
            yield break;
        }

        Cell cellA = gridManager.targetWordCells
            .First(c => c.coordinates == coords[0]);
        Cell cellB = gridManager.targetWordCells
            .First(c => c.coordinates == coords[1]);

        inputLocked = false;
        SynapseGameManager.Instance.inputLocked = false;

        var canvas = fingerImage.canvas;
        var canvasRT = canvas.GetComponent<RectTransform>();
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        ? null
                        : canvas.worldCamera;

        Vector2 screenA = RectTransformUtility.WorldToScreenPoint(cam, cellA.transform.position);
        Vector2 screenB = RectTransformUtility.WorldToScreenPoint(cam, cellB.transform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenA, cam, out Vector2 localA);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenB, cam, out Vector2 localB);

        while (true)
        {
            yield return StartCoroutine(SingleGesture(localA, localB));

            var sel = SynapseGameManager.Instance.selectedCells;
            if (sel.Count >= 2 && sel[0] == cellA && sel[1] == cellB)
                break;
        }

        fingerImage.gameObject.SetActive(false);
        UnlockInput();
    }

    private IEnumerator SingleGesture(Vector2 localA, Vector2 localB)
    {
        fingerImage.gameObject.SetActive(true);

        fingerImage.rectTransform.anchoredPosition = localA;
        yield return PulseFinger();

        float dur = 0.6f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            fingerImage.rectTransform.anchoredPosition = Vector2.Lerp(localA, localB, t / dur);
            yield return null;
        }
        fingerImage.rectTransform.anchoredPosition = localB;

        yield return PulseFinger();

        yield return new WaitForSeconds(0.2f);
    }

    /// <summary>
    /// FingerImage üzerinde küçülüp büyüyen tap efekti.
    /// </summary>
    private IEnumerator PulseFinger()
    {
        float dur = 0.3f;
        float elapsed = 0f;
        Vector3 baseScale = fingerImage.rectTransform.localScale;
        Vector3 maxScale = baseScale * 1.2f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float f = Mathf.Sin((elapsed / dur) * Mathf.PI);
            fingerImage.rectTransform.localScale = Vector3.Lerp(baseScale, maxScale, f);
            yield return null;
        }
        fingerImage.rectTransform.localScale = baseScale;
    }

    private void UnlockInput()
    {
        inputLocked = false;
        SynapseGameManager.Instance.inputLocked = false;
        SynapseGameManager.Instance.lineDrawer.enabled = true;
    }

    private void UnhighlightAll()
    {
        // puls durdur
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        foreach (var c in gridManager.GetComponentsInChildren<Cell>())
        {
            c.ResetVisual();

            c.transform.localScale = Vector3.one;
        }
    }

    private Cell FindCellWithFeature(Feature f)
    {
        foreach (var c in gridManager.GetComponentsInChildren<Cell>())
        {
            switch (f)
            {
                case Feature.Locked:
                    if (c.isLocked) return c;
                    break;
                case Feature.Hidden:
                    if (c.isHidden && !c.isRevealed) return c;
                    break;
                case Feature.Danger:
                    if (c.isDanger) return c;
                    break;
                case Feature.DirectionBlock:
                    if (c.blockedDirections.Count > 0) return c;
                    break;
                case Feature.Swap:
                    if (gridManager.allowSwap && !c.isLocked && !c.isHidden && !c.isDanger)
                        return c;
                    break;
                case Feature.Timer:
                    return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Hücreyi 1.0↔1.2 arası ping-pong yaparak pulse’lar.
    /// </summary>
    private IEnumerator PulseCell(Transform cellTf)
    {
        float duration = 0.5f;
        Vector3 baseScale = cellTf.localScale;
        Vector3 maxScale = baseScale * 1.2f;
        float t = 0f;

        while (true)
        {
            if (cellTf == null)
                yield break;

            t += Time.deltaTime;
            // 0→1→0 arasında döngü
            float f = Mathf.PingPong(t / duration, 1f);
            cellTf.localScale = Vector3.Lerp(baseScale, maxScale, f);
            yield return null;
        }
    }
}
