using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Her seviye için harita oluşturur ve hedef kelime yerleştirme,
/// hücre oluşturma, ipuçları ve rastgele hücre takaslarını yönetir.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Izgara Ayarları")]
    [Tooltip("Her hücre için kullanılacak prefab.")]
    public GameObject cellPrefab;
    [Tooltip("Izgaranın boyutu (gridSize x gridSize).")]
    public int gridSize = 6;
    [Tooltip("Hücreler arası boşluk.")]
    public float spacing = 110f;

    [Header("Seviye Özellikleri")]
    [Tooltip("Rastgele iki hücrenin yerlerini takas etme izni.")]
    public bool allowSwap;
    [Tooltip("Kilitli hücrelerin dahil edilip edilmemesi.")]
    private bool allowLockedCells;
    [Tooltip("Yön bloklarının dahil edilip edilmemesi.")]
    private bool allowDirectionBlocks;
    [Tooltip("Tehlike oluşturacak hücrelerin dahil edilip edilmemesi.")]
    private bool allowDangerCells;
    [Tooltip("Gizli hücrelerin dahil edilip edilmemesi.")]
    private bool allowHiddenCells;
    [Tooltip("Timer.")]
    private bool enableTimer;
    // public bool allowSwap, allowLockedCells, allowHiddenCells, allowDangerCells, allowDirectionBlocks, enableTimer;
    private float swapProbability, lockedProbability, hiddenProbability, dangerProbability, directionProbability;

    [Header("Hint Overlay Sprite")]
    [Tooltip("Hint ve açılışta rastgele harf gösterme için kullanılacak sprite")]
    public Sprite hintOverlaySprite;
    [Header("İpucu Ayarları")]
    [Tooltip("Her harfin yanıp sönme süresi (saniye).")]
    public float hintBlinkDuration = 0.3f;
    private GameObject hintOverlayInstance;

    // Izgaradaki harfleri saklayan dizi
    private string[,] gridLetters;
    // Hedef kelime hücrelerine ait Cell bileşenleri
    public readonly List<Cell> targetWordCells = new List<Cell>();
    // Hedef kelime hücrelerinin koordinatları
    public readonly List<Vector2Int> targetWordCoords = new List<Vector2Int>();

    // Rastgele harf üretimi için kullanılacak karakter dizisi
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // GenerateGrid içinde doldurmak için…
    private Cell[,] cellGrid;

    private void Start()
    {
        // Eğer hücre takası aktifse, belirli aralıklarla rasgele takas yap
        if (allowSwap)
            InvokeRepeating(nameof(SwapRandomCells), 3f, 6f);
    }

    /// <summary>
    /// Seviye verilerine göre ızgara ayarlarını uygular ve ızgarayı oluşturur.
    /// </summary>
    public void ApplyLevelSettings(LevelData data, string word)
    {
        gridSize = data.gridSize;
        enableTimer = data.enableTimer;

        // swap
        allowSwap = data.enableSwap;
        swapProbability = data.swapProbability;

        // locked
        allowLockedCells = data.includeLockedCells;
        lockedProbability = data.lockedProbability;

        // hidden
        allowHiddenCells = data.includeHiddenCells;
        hiddenProbability = data.hiddenProbability;

        // danger
        allowDangerCells = data.includeDangerCells;
        dangerProbability = data.dangerProbability;

        // direction blocks
        allowDirectionBlocks = data.includeDirectionBlocks;
        directionProbability = data.directionProbability;

        // … önceki selection reset, lineDrawer.ClearPath vs. …

        GenerateGrid(word);

        // swap coroutine’ini her level başında resetle, sonra olasılığa göre başlat
        CancelInvoke(nameof(SwapRandomCells));
        if (allowSwap && Random.value < swapProbability)
            InvokeRepeating(nameof(SwapRandomCells), 3f, 6f);
    }

    /// <summary>
    /// Mevcut ızgarayı temizler, harfleri atar, hedef kelime yerleşimini yapar,
    /// hücreleri instantiate eder ve ilk ipucunu gösterir.
    /// </summary>
    private void GenerateGrid(string word)
    {
        cellGrid = new Cell[gridSize, gridSize];
        StopAllCoroutines();

        foreach (Transform child in transform)
            Destroy(child.gameObject);
        targetWordCells.Clear();
        targetWordCoords.Clear();
        SynapseGameManager.Instance.ResetSelection();
        SynapseGameManager.Instance.lineDrawer.ClearPath();

        gridLetters = new string[gridSize, gridSize];
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                gridLetters[x, y] = GetRandomLetter();

        PlaceWordInGrid(word);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                string letter = gridLetters[x, y];
                bool isTarget = targetWordCoords.Contains(coord);

                bool isLocked = allowLockedCells && !isTarget && Random.value < lockedProbability;
                bool isHidden = allowHiddenCells && !isTarget && Random.value < hiddenProbability;
                bool isDanger = allowDangerCells && !isTarget && Random.value < dangerProbability;

                var cellGO = Instantiate(cellPrefab, transform);
                var rt = cellGO.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x * spacing, -y * spacing);
                cellGO.name = $"Cell_{x}_{y}";

                var cell = cellGO.GetComponent<Cell>();
                cellGrid[x, y] = cell;
                cell.Setup(coord, letter, isLocked, isHidden, isDanger);

                if (allowDirectionBlocks && !isTarget && Random.value < directionProbability)
                    ApplyRandomDirectionBlocks(cell);

                if (isTarget)
                    targetWordCells.Add(cell);

                cell.UpdateVisual();
            }
        }

        ShowLetterHint();
    }

    /// <summary>
    /// Tüm hücreleri soldan sağa, yukarıdan aşağıya doğru
    /// sırasıyla seçili rengine dönüştürüp delay’li gösterir.
    /// </summary>
    public IEnumerator HighlightAllSequentially(float delay)
    {
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var c = cellGrid[x, y];
                c.isSelected = true;
                c.UpdateVisual();
                yield return new WaitForSeconds(delay);
            }
        }
    }

    /// <summary>
    /// Rastgele bir harf üretir.
    /// </summary>
    private string GetRandomLetter()
    {
        int idx = Random.Range(0, Letters.Length);
        return Letters[idx].ToString();
    }

    /// <summary>
    /// Kelimeyi komşu hücrelere yerleştirmeye çalışır; başarısızsa rasgele yerleştirir.
    /// </summary>
    private void PlaceWordInGrid(string word)
    {
        int tries = 0;
        bool placed = false;

        while (!placed && tries++ < 100)
        {
            targetWordCoords.Clear();
            var path = new List<Vector2Int>();
            var pos = new Vector2Int(Random.Range(0, gridSize), Random.Range(0, gridSize));
            path.Add(pos);
            bool valid = true;

            for (int i = 1; i < word.Length; i++)
            {
                var neighbors = GetAdjacentFreeCells(pos, path);
                if (neighbors.Count == 0) { valid = false; break; }
                pos = neighbors[Random.Range(0, neighbors.Count)];
                path.Add(pos);
            }

            if (valid && path.Count == word.Length)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    var c = path[i];
                    gridLetters[c.x, c.y] = word[i].ToString();
                    targetWordCoords.Add(c);
                }
                placed = true;
            }
        }

        if (!placed)
        {
            Debug.LogWarning("Kelime yan yana yerleştirilemedi, rasgele konumlandırılıyor.");
            targetWordCoords.Clear();
            var used = new HashSet<Vector2Int>();
            for (int i = 0; i < word.Length; i++)
            {
                Vector2Int c;
                do { c = new Vector2Int(Random.Range(0, gridSize), Random.Range(0, gridSize)); }
                while (used.Contains(c));
                used.Add(c);
                gridLetters[c.x, c.y] = word[i].ToString();
                targetWordCoords.Add(c);
            }
        }
    }

    /// <summary>
    /// Verilen hücreye 1-3 rasgele yön bloğu ekler.
    /// </summary>
    private void ApplyRandomDirectionBlocks(Cell cell)
    {
        var dirs = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        int count = Random.Range(1, 4);
        for (int i = 0; i < count && dirs.Count > 0; i++)
        {
            int idx = Random.Range(0, dirs.Count);
            cell.blockedDirections.Add(dirs[idx]);
            dirs.RemoveAt(idx);
        }
    }

    /// <summary>
    /// Komşu hücreleri (2 hücre yarıçapında) döndürür.
    /// </summary>
    private List<Vector2Int> GetAdjacentFreeCells(Vector2Int from, List<Vector2Int> used)
    {
        var list = new List<Vector2Int>();
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var n = new Vector2Int(from.x + dx, from.y + dy);
                if (n.x >= 0 && n.x < gridSize && n.y >= 0 && n.y < gridSize && !used.Contains(n))
                    list.Add(n);
            }
        return list;
    }

    /// <summary>
    /// Hedef kelime hücrelerinden birini yanıp sönme ile gösterir.
    /// </summary>
    private void ShowLetterHint()
    {
        if (targetWordCells.Count == 0) return;
        int idx = Random.Range(0, targetWordCells.Count);
        StartCoroutine(FlashHint(targetWordCells[idx]));
    }

    /// <summary>
    /// Hücrenin rengini değiştirerek ipucu verir, ardından eski haline döner.
    /// </summary>
    private IEnumerator FlashHint(Cell cell)
    {
        CreateHintOverlay(cell.transform);

        yield return new WaitForSeconds(hintBlinkDuration);

        DestroyHintOverlay();
    }

    /// <summary>
    /// Yeniden başlat butonuna basıldığında mevcut seviyeyi yeniden oluşturur.
    /// </summary>
    public void OnRestartClicked()
    {
        StopAllCoroutines();
        var lvlMgr = LevelManager.Instance;
        var data = lvlMgr.allLevels[lvlMgr.currentLevelIndex];
        var word = data.targetWords[lvlMgr.currentWordIndex];
        ApplyLevelSettings(data, word);
    }

    /// <summary>
    /// İpucu butonuna basıldığında tüm kelimeyi sırayla yanıp söndürerek gösterir.
    /// </summary>
    public void OnHintClicked()
    {
        StopAllCoroutines();
        StartCoroutine(ShowFullWordHint());
    }

    private IEnumerator ShowFullWordHint()
    {
        float half = hintBlinkDuration / 2f;
        foreach (var coord in targetWordCoords)
        {
            var cell = targetWordCells.Find(c => c.coordinates == coord);
            if (cell != null)
            {
                CreateHintOverlay(cell.transform);
                yield return new WaitForSeconds(half);
                DestroyHintOverlay();
                yield return new WaitForSeconds(half);
            }
        }
    }

    /// <summary>
    /// Hücre üzerine tam oturan bir Image GameObject yaratır.
    /// </summary>
    private void CreateHintOverlay(Transform cellTf)
    {
        DestroyHintOverlay();

        hintOverlayInstance = new GameObject("HintOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hintOverlayInstance.transform.SetParent(cellTf, false);

        var rt = hintOverlayInstance.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = hintOverlayInstance.GetComponent<Image>();
        img.sprite = hintOverlaySprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    private void DestroyHintOverlay()
    {
        if (hintOverlayInstance != null)
        {
            Destroy(hintOverlayInstance);
            hintOverlayInstance = null;
        }
    }

    /// <summary>
    /// İzin varsa rastgele iki hücrenin pozisyonlarını animasyonlu şekilde takas eder.
    /// </summary>
    public void SwapRandomCells()
    {
        if (!allowSwap) return;

        var cells = new List<Cell>(GetComponentsInChildren<Cell>());
        if (cells.Count < 2) return;

        int a = Random.Range(0, cells.Count), b;
        do { b = Random.Range(0, cells.Count); } while (b == a);

        var cellA = cells[a];
        var cellB = cells[b];

        if (targetWordCoords.Contains(cellA.coordinates) || targetWordCoords.Contains(cellB.coordinates))
        {
            SynapseGameManager.Instance.ResetSelection();
            SynapseGameManager.Instance.lineDrawer.ClearPath();
        }

        StartCoroutine(SwapVisual(cellA, cellB));
        SwapLetters(cellA, cellB);
        cellA.UpdateVisual();
        cellB.UpdateVisual();
    }

    /// <summary>
    /// İki hücre arasındaki harfleri ve metni takas eder.
    /// </summary>
    private void SwapLetters(Cell a, Cell b)
    {
        string temp = a.letter;
        a.letter = b.letter;
        b.letter = temp;
        a.UpdateText();
        b.UpdateText();
    }

    /// <summary>
    /// Hücrelerin pozisyonlarını 0.4 saniyede animasyonlu olarak değiştirir.
    /// </summary>
    private IEnumerator SwapVisual(Cell a, Cell b)
    {
        Vector3 posA = a.transform.localPosition;
        Vector3 posB = b.transform.localPosition;
        float duration = 0.4f, elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (a == null || b == null) yield break;
            float t = elapsed / duration;
            a.transform.localPosition = Vector3.Lerp(posA, posB, t);
            b.transform.localPosition = Vector3.Lerp(posB, posA, t);
            yield return null;
        }

        if (a != null && b != null)
        {
            a.transform.localPosition = posB;
            b.transform.localPosition = posA;
        }
    }
}