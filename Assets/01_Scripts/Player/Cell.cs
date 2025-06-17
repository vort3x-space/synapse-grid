using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Tek bir hücreyi yönetir: durumu, görselliği ve etkileşimleri.
/// </summary>
public class Cell : MonoBehaviour
{
    [Tooltip("Izgara koordinatları")]
    public Vector2Int coordinates;

    [Tooltip("Bu hücredeki harf")]
    public string letter;

    [Header("Durum Bayrakları")]
    [Tooltip("Hücre seçili mi?")]
    public bool isSelected;

    [Tooltip("Kilitleme durumu")]
    public bool isLocked;

    [Tooltip("Gizli hücre mi?")]
    public bool isHidden;

    private bool dangerRevealed;
    public bool isRevealed;

    [Tooltip("Tehlikeli hücre mi?")]
    public bool isDanger;

    [Header("Yön Engelleri")]
    [Tooltip("Bu hücredeki engellenmiş yönler")]
    public List<Direction> blockedDirections = new List<Direction>();

    [Header("Ok Gösterimleri")]
    public GameObject arrowUp;
    public GameObject arrowDown;
    public GameObject arrowLeft;
    public GameObject arrowRight;

    [Header("Sprite Ayarları")]
    public Sprite normalBoxSprite;
    public Sprite selectedBoxSprite;
    public Sprite lockedBoxSprite;
    public Sprite hiddenBoxSprite;
    public Sprite dangerBoxSprite;

    private float defaultOutlineWidth;
    private Color defaultOutlineColor;
    private Button button;
    private Image bg;
    private TMP_Text label;

    private void Awake()
    {
        button = GetComponent<Button>();
        bg = GetComponent<Image>();
        label = GetComponentInChildren<TMP_Text>();
        button.onClick.AddListener(OnClick);

        var matInstance = Instantiate(label.fontMaterial);
        label.fontMaterial = matInstance;

        // Varsayılan outline değerlerini al
        defaultOutlineWidth = matInstance.GetFloat(ShaderUtilities.ID_OutlineWidth);
        defaultOutlineColor = matInstance.GetColor(ShaderUtilities.ID_OutlineColor);

        dangerRevealed = false;
    }

    /// <summary>
    /// Hücreyi verilen parametrelere göre ayarlar.
    /// </summary>
    public void Setup(Vector2Int coord, string letter, bool locked = false, bool hidden = false, bool danger = false)
    {
        coordinates = coord;
        this.letter = letter;
        isSelected = false;
        isLocked = locked;
        isHidden = hidden;
        isDanger = danger;
        isRevealed = !hidden;

        label.text = isRevealed ? letter : "?";
        UpdateVisual();
    }

    /// <summary>
    /// Hücre tıklandığında çağrılır: gizli açma ve seçim işleyişi.
    /// </summary>
    private void OnClick()
    {
        if (SynapseGameManager.Instance.inputLocked)
            return;

        UIManager.Instance.PlayCellClick();

        if (isSelected) return;

        if (isDanger && !dangerRevealed)
        {
            dangerRevealed = true;

            label.text = letter;
            UpdateVisual();
            return;
        }

        if (isHidden && !isRevealed)
        {
            isRevealed = true;
            label.text = letter;
            UpdateVisual();
            return;
        }

        isSelected = true;
        SynapseGameManager.Instance.SelectCell(this);
        UpdateVisual();
    }

    /// <summary>
    /// Hücrenin görsel durumunu günceller.
    /// </summary>
    public void UpdateVisual()
    {
        // Harf metni
        if (!isDanger || (isDanger && dangerRevealed))
            label.text = isRevealed || dangerRevealed ? letter : "?";
        else
            label.text = "!";

        arrowUp?.SetActive(blockedDirections.Contains(Direction.Up));
        arrowDown?.SetActive(blockedDirections.Contains(Direction.Down));
        arrowLeft?.SetActive(blockedDirections.Contains(Direction.Left));
        arrowRight?.SetActive(blockedDirections.Contains(Direction.Right));

        if (isSelected)
        {
            bg.sprite = selectedBoxSprite;
            label.color = Color.black;

            label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
            label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.white);

            // Harf rengi #008200
            label.color = new Color32(0x00, 0x82, 0x00, 0xFF);

            return;
        }

        if (isDanger && dangerRevealed)
        {
            bg.sprite = dangerBoxSprite;
            label.color = new Color32(0xFF, 0x1E, 0x1E, 0xFF);
            return;
        }
        if (isDanger && !dangerRevealed)
        {
            bg.sprite = normalBoxSprite;
            label.color = Color.black;
            return;
        }

        if (isLocked)
        {
            bg.sprite = lockedBoxSprite;
            label.color = Color.clear;
            HideArrows();
            return;
        }

        if (isHidden && !isRevealed)
        {
            bg.sprite = hiddenBoxSprite;
            label.color = Color.white;
            return;
        }
        bg.sprite = normalBoxSprite;
        label.color = Color.black;

        label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, defaultOutlineWidth);
        label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, defaultOutlineColor);

    }

    /// <summary>
    /// Ok nesnelerini gizler (kilitli hücreler için).
    /// </summary>
    private void HideArrows()
    {
        arrowUp?.SetActive(false);
        arrowDown?.SetActive(false);
        arrowLeft?.SetActive(false);
        arrowRight?.SetActive(false);
    }

    /// <summary>
    /// Hücreyi geçici olarak renklendirir (ipucu veya başarı vurgusu).
    /// </summary>
    public void Highlight(Color color)
    {
        bg.color = color;
    }

    /// <summary>
    /// Görünümü varsayılan haline döndürür.
    /// </summary>
    public void ResetVisual()
    {
        bg.color = isSelected ? Color.cyan : Color.white;
        // Harf outline’ını da sıfırla
        label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, defaultOutlineWidth);
        label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, defaultOutlineColor);
    }

    /// <summary>
    /// Harf metnini günceller.
    /// </summary>
    public void UpdateText()
    {
        label.text = letter;
    }

    /// <summary>
    /// Hücreyi belirtilen süre boyunca döndürür.
    /// </summary>
    public void Spin(float duration = 0.5f)
    {
        StartCoroutine(SpinRoutine(duration));
    }

    private IEnumerator SpinRoutine(float duration)
    {
        float elapsed = 0f;
        float startAngle = transform.localEulerAngles.z;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Lerp(startAngle, startAngle + 360f, elapsed / duration);
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        transform.localRotation = Quaternion.identity;
    }
}