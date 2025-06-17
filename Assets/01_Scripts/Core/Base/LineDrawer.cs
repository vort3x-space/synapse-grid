using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
/// <summary>
/// Hücreler arasındaki seçili yola çizgi çizer, renk ve animasyon işlemlerini yönetir.
/// </summary>
public class LineDrawer : MonoBehaviour
{
    [Tooltip("Aktifse çizgi kaybolma animasyonu sürüyor.")]
    private bool isFading = false;
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    /// <summary>Kendini ve varolan yolu gizler.</summary>
    public void HideLine()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }

    /// <summary>Çizime izin verir (önceki yolu da siler).</summary>
    public void ShowLine()
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Hücre listesini alıp aralarına çizgi çizer.
    /// </summary>
    /// <param name="cells">Çizgi oluşturulacak hücreler.</param>
    public void DrawPath(List<Cell> cells)
    {
        lineRenderer.positionCount = cells.Count;
        for (int i = 0; i < cells.Count; i++)
        {
            RectTransform rt = cells[i].GetComponent<RectTransform>();

            Vector3 worldPos = rt.TransformPoint(rt.rect.center);

            worldPos.z = rt.position.z - 0.01f;  // hücre z=0 ise çizgi z=-0.01

            lineRenderer.SetPosition(i, worldPos);
        }
        Debug.Log($"Çizildi: {cells.Count} hücre | Alpha: {lineRenderer.startColor.a}");
    }

    /// <summary>
    /// Mevcut çizgiyi tamamen temizler.
    /// </summary>
    public void ClearPath()
    {
        lineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Çizginin rengini anında ayarlar.
    /// </summary>
    /// <param name="color">Yeni renk (alpha 1 olarak uygulanır).</param>
    public void SetLineColor(Color color)
    {
        color.a = 1f;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    /// <summary>
    /// Süre boyunca çizgiyi giderek şeffaflaştırarak kaybolmasını sağlar.
    /// </summary>
    /// <param name="duration">Animasyon süresi (saniye).</param>
    public IEnumerator FadeOutLine(float duration)
    {
        isFading = true;
        Color startColor = lineRenderer.startColor;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color lerped = Color.Lerp(startColor, targetColor, elapsed / duration);
            lineRenderer.startColor = lerped;
            lineRenderer.endColor = lerped;
            yield return null;
        }

        ClearPath();
        isFading = false;
    }

    /// <summary>
    /// Çizgi boyunca ışıldama (glow) animasyonu başlatır.
    /// </summary>
    public void AnimateLineGlow()
    {
        StartCoroutine(AnimateLine());
    }

    /// <summary>
    /// Kısa renk değişimleriyle çizgiye ışıldama efekti verir, ardından yeşil renge döner.
    /// </summary>
    private IEnumerator AnimateLine()
    {
        float duration = 0.5f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float ping = Mathf.PingPong(t * 5f, 1f);
            Color glow = Color.Lerp(Color.white, Color.cyan, ping);
            glow.a = 1f;
            lineRenderer.startColor = glow;
            lineRenderer.endColor = glow;
            yield return null;
        }

        Color finalColor = Color.green;
        finalColor.a = 1f;
        lineRenderer.startColor = finalColor;
        lineRenderer.endColor = finalColor;
    }
}


//  sonra kullanılır
// public void PlaySuccessFX(List<Cell> path)
// {
//     if (lineSuccessFXPrefab == null || path.Count == 0) return;

//     // Çizgi boyunca FX patlat
//     foreach (var cell in path)
//     {
//         GameObject fx = Instantiate(lineSuccessFXPrefab, cell.transform.position, Quaternion.identity);
//         Destroy(fx, 2f); // otomatik yok et
//     }
// }