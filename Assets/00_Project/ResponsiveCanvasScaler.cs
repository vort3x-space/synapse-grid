using UnityEngine;
using UnityEngine.UI;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

[RequireComponent(typeof(CanvasScaler))]
public class ResponsiveCanvasScaler : MonoBehaviour
{
        [Tooltip("Telefonlarda kullanılacak match değeri")]
        public float phoneMatch = 0.5f;
        [Tooltip("Tabletlerde kullanılacak match değeri")]
        public float tabletMatch = 0.3f;

        private CanvasScaler _scaler;
        private int _lastWidth;
        private int _lastHeight;

        void Awake()
        {
                _scaler = GetComponent<CanvasScaler>();
                // Başlangıçta bir kez uygula:
                UpdateMatch();
                // O anki boyutu kaydet
                _lastWidth = Screen.width;
                _lastHeight = Screen.height;
        }

        void Update()
        {
                // Ekran boyutu değiştiyse yeniden hesapla
                if (Screen.width != _lastWidth || Screen.height != _lastHeight)
                {
                        _lastWidth = Screen.width;
                        _lastHeight = Screen.height;
                        UpdateMatch();
                }
        }

        private void UpdateMatch()
        {
                bool isiPad = false;

#if UNITY_IOS
        isiPad = Device.generation.ToString().StartsWith("iPad");
#else
                // Editörde ya da Android’de aspect farkıyla kabaca tablet mi diye bak:
                float aspect = (float)Screen.width / Screen.height;
                isiPad = aspect > 0.7f;
#endif

                _scaler.matchWidthOrHeight = isiPad ? tabletMatch : phoneMatch;
                Debug.Log($"[ResponsiveScaler] {Screen.width}×{Screen.height}  isiPad={isiPad}  match={_scaler.matchWidthOrHeight}");
        }
}
