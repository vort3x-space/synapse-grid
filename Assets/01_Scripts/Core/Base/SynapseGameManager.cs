using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Oyun akışını, hücre seçimini ve kelime kontrolünü yöneten ana sınıf.
/// </summary>
public class SynapseGameManager : MonoBehaviour
{
    public static SynapseGameManager Instance { get; private set; }

    [Tooltip("Seçilen hücreleri saklayan liste.")]
    public List<Cell> selectedCells = new List<Cell>();

    [Tooltip("Mevcut hedef kelime.")]
    public string targetWord = "WORD";

    [Tooltip("Çizgi çizme işlemlerini yöneten bileşen.")]
    public LineDrawer lineDrawer;

    [Header("VFX Prefabs")]
    [Tooltip("Doğru segment çizildiğinde oynatılacak VFX'ler (rastgele seçilecek)")]
    public List<GameObject> segmentVFXPrefabs;
    [Tooltip("Kelime tamamlandığında oynatılacak VFX")]
    public GameObject completionVFXPrefab;

    // Girdi kilitli olduğunda seçim yapılamaz
    public bool inputLocked = false;

    /// <summary>
    /// Çoğaltma varsa yeni nesneyi yok eder.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Hücre seçildiğinde çağrılır; doğrulama, vuruş kontrolü ve ilerleme burada yönetilir.
    /// </summary>
    /// <param name="cell">Seçilen hücrenin Cell bileşeni.</param>
    public void SelectCell(Cell cell)
    {
        if (inputLocked)
            return;

        if (cell.isLocked)
        {
            ResetSelection();
            lineDrawer.ClearPath();
            return;
        }

        if (selectedCells.Contains(cell))
            return;

        if (selectedCells.Count > 0)
        {
            Cell last = selectedCells[selectedCells.Count - 1];
            Direction dir = GetDirection(last.coordinates, cell.coordinates);
            if (last.blockedDirections.Contains(dir) ||
                cell.blockedDirections.Contains(GetOppositeDirection(dir)))
            {
                UIManager.Instance.ShowDirectionWarning();
                cell.isSelected = false;
                cell.UpdateVisual();
                ResetSelection();
                lineDrawer.ClearPath();
                return;
            }
        }

        selectedCells.Add(cell);

        if (cell.isDanger)
        {
            ResetSelection();
            lineDrawer.ClearPath();
            return;
        }

        cell.isSelected = true;
        cell.UpdateVisual();

        string current = string.Concat(selectedCells.ConvertAll(c => c.letter));
        lineDrawer.DrawPath(selectedCells);

        if (targetWord.StartsWith(current))
        {
            bool isComplete = current == targetWord;
            lineDrawer.SetLineColor(isComplete ? Color.green : Color.yellow);

            if (!isComplete && selectedCells.Count >= 2 && segmentVFXPrefabs.Count > 0)
            {
                int rnd = Random.Range(0, segmentVFXPrefabs.Count);
                var prefab = segmentVFXPrefabs[rnd];

                Vector3 a = selectedCells[selectedCells.Count - 2].transform.position;
                Vector3 b = selectedCells[selectedCells.Count - 1].transform.position;
                Vector3 mid = (a + b) * 0.5f;

                var vfx = Instantiate(prefab, mid, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            if (isComplete)
            {
                inputLocked = true;
                UIManager.Instance.StopTimer();
                UIManager.Instance.ShakeCamera();
                UIManager.Instance.PlaySuccessSound();

                int idx = LevelManager.Instance.currentWordIndex;
                UIManager.Instance.MarkWordComplete(idx);

                lineDrawer.AnimateLineGlow();

                if (completionVFXPrefab != null)
                {
                    Vector3 center = Vector3.zero;
                    foreach (var c in selectedCells)
                        center += c.transform.position;
                    center /= selectedCells.Count;

                    var vfxAll = Instantiate(completionVFXPrefab, center, Quaternion.identity);
                    Destroy(vfxAll, 3f);
                }
                UIManager.Instance.PlayVictorySound();

                Invoke(nameof(BeginWordTransition), 2f);
            }
        }
        else
        {
            var copy = new List<Cell>(selectedCells);
            ResetSelection();
            lineDrawer.DrawPath(copy);
            lineDrawer.SetLineColor(Color.red);
            StartCoroutine(lineDrawer.FadeOutLine(1f));
        }
    }

    private void BeginWordTransition()
    {
        StartCoroutine(WordTransitionCoroutine());
    }

    /// <summary>
    /// Önce grid’i soldan sağa highlight’la,
    /// sonra aynı level’daki sıradaki kelimeye geçiş yap.
    /// </summary>
    private IEnumerator WordTransitionCoroutine()
    {
        var lvl = LevelManager.Instance;
        int lvlIdx = lvl.currentLevelIndex;
        int wordIdx = lvl.currentWordIndex;

        yield return StartCoroutine(
            lvl.gridManager.HighlightAllSequentially(0.04f)
        );

        inputLocked = false;
        ResetSelection();
        lineDrawer.ClearPath();

        if (wordIdx < lvl.allLevels[lvlIdx].targetWords.Count - 1)
        {
            lvl.LoadLevel(lvlIdx, wordIdx + 1);

        }
        else
        {
            GameUIManager.Instance.ShowVictory();
        }
    }

    /// <summary>
    /// Sonraki kelime veya seviyeyi yükler.
    /// </summary>
    public void LoadNextLevel()
    {
        inputLocked = false;
        ResetSelection();
        lineDrawer.ClearPath();

        LevelManager.Instance.LoadNextLevel();
    }

    /// <summary>
    /// Tüm seçimi temizler ve hücrelerin görünümlerini eski haline döndürür.
    /// </summary>
    public void ResetSelection()
    {
        foreach (var c in selectedCells)
        {
            c.isSelected = false;
            c.UpdateVisual();
        }
        selectedCells.Clear();
    }

    /// <summary>
    /// Süre dolduğunda kullanıcı girişi kilitler.
    /// </summary>
    public void OnTimeOver()
    {
        inputLocked = true;
        UIManager.Instance.PlayDefeatSound();
        GameUIManager.Instance.ShowDefeat();
    }

    /// <summary>
    /// İki koordinat arasındaki yönü döndürür.
    /// </summary>
    /// <param name="from">Başlangıç koordinatı.</param>
    /// <param name="to">Hedef koordinatı.</param>
    /// <returns>Hareket yönü.</returns>
    private Direction GetDirection(Vector2Int from, Vector2Int to)
    {
        if (to.x == from.x)
            return to.y > from.y ? Direction.Down : Direction.Up;
        return to.x > from.x ? Direction.Right : Direction.Left;
    }

    /// <summary>
    /// Verilen yönün zıt yönünü döndürür.
    /// </summary>
    private Direction GetOppositeDirection(Direction dir) => dir switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => dir
    };
}



// private void OnWordComplete()
// {
//     var lvl = LevelManager.Instance;
//     bool isLastWord = lvl.currentWordIndex ==
//         lvl.allLevels[lvl.currentLevelIndex].targetWords.Count - 1;

//     if (isLastWord)
//     {
//         // Seviye sonu:
//         GameUIManager.Instance.ShowVictory();
//     }
//     else
//     {
//         LoadNextLevel();
//     }
// }