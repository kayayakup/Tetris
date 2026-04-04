using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundManager : MonoBehaviour
{
    public Sprite[] backgroundSprites;
    public float changeInterval = 10f;
    public float crossfadeDuration = 2f;
    public float maxAlpha = 0.05f; // Maksimum görünürlük seviyesi

    public Image _bg1;
    public Image _bg2;
    private bool _usingBg1 = true;
    private int _currentIndex = 0;

    private void Awake()
    {
        // Null checks added to prevent Editor errors if Images are not assigned yet
        if (_bg1 == null || _bg2 == null)
        {
            Debug.LogWarning("[BackgroundManager] Lütfen Inspector üzerinden _bg1 ve _bg2 Image'lerini atayın.");
            return;
        }

        // Set initial alpha
        Color c2 = _bg2.color;
        c2.a = 0f;
        _bg2.color = c2;

        Color c1 = _bg1.color;
        c1.a = maxAlpha;
        _bg1.color = c1;

        if (backgroundSprites != null && backgroundSprites.Length > 0)
        {
            _bg1.sprite = backgroundSprites[0];
            _bg2.sprite = backgroundSprites[0]; // Avoid empty white sprite initially
        }
    }

    private void Start()
    {
        if (backgroundSprites != null && backgroundSprites.Length > 1 && _bg1 != null && _bg2 != null)
        {
            StartCoroutine(BackgroundRoutine());
        }
    }

    private IEnumerator BackgroundRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(changeInterval);

            _currentIndex = (_currentIndex + 1) % backgroundSprites.Length;
            Sprite nextSprite = backgroundSprites[_currentIndex];

            if (_usingBg1)
            {
                _bg2.sprite = nextSprite;
                _bg2.transform.SetAsLastSibling(); // Ensure fading in image is drawn on top inside Canvas
                yield return StartCoroutine(Crossfade(_bg1, _bg2));
            }
            else
            {
                _bg1.sprite = nextSprite;
                _bg1.transform.SetAsLastSibling();
                yield return StartCoroutine(Crossfade(_bg2, _bg1));
            }

            _usingBg1 = !_usingBg1;
        }
    }

    private IEnumerator Crossfade(Image fadingOut, Image fadingIn)
    {
        float elapsed = 0f;
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossfadeDuration;
            
            Color cIn = fadingIn.color;
            // t goes from 0 to 1, we map it to 0 to maxAlpha
            cIn.a = t * maxAlpha;
            fadingIn.color = cIn;

            yield return null;
        }

        // Hard set final alphas
        Color finalIn = fadingIn.color;
        finalIn.a = maxAlpha;
        fadingIn.color = finalIn;

        Color finalOut = fadingOut.color;
        finalOut.a = 0f; 
        fadingOut.color = finalOut;
    }
}
