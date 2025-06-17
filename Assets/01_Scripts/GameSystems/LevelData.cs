using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tek bir Synapse Grid seviyesi için tüm yapılandırma verilerini tutar.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "SynapseGrid/Level", order = 0)]
public class LevelData : ScriptableObject
{
    // Seviyenin gösterilecek adı
    public string levelName;

    // Kare ızgaranın (gridSize x gridSize) boyutu
    public int gridSize = 6;

    // Aktifse, seviyede geri sayım sayacı çalışır
    public bool enableTimer = false;
    [Tooltip("Timer aktifse, başlangıçta kaç saniye verilsin?")]
    public float timerDuration = 60f;    // ← yen eklendi

    // Aktifse, hücrelerin oyuncu tarafından yer değiştirilmesine izin verir
    public bool enableSwap = false;
    [Range(0f, 1f)] public float swapProbability = 1f;
    // swapProbability: 0–1 arasında. 1 ise her seviye her zaman swap aktif.

    // Aktifse, kilitli hücreler seviyede yer alır
    [Header("Cell Feature Inclusion")]
    public bool includeLockedCells = false;
    [Range(0f, 1f)] public float lockedProbability = 0.1f;

    public bool includeHiddenCells = false;
    [Range(0f, 1f)] public float hiddenProbability = 0.15f;

    public bool includeDangerCells = false;
    [Range(0f, 1f)] public float dangerProbability = 0.1f;

    public bool includeDirectionBlocks = false;
    [Range(0f, 1f)] public float directionProbability = 0.07f;

    [Header("Target")]
    public List<string> targetWords = new List<string>();
}