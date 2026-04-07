using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles cool particle effects, text popups, and sounds for line clears.
/// Attach this script to your GameManager (or any object in the scene) to configure the Inspector fields.
/// </summary>
public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;
    public static VFXManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<VFXManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("VFXManager not found in scene! A temporary one will be created. Please attach the VFXManager script to an object in the scene to configure its fields.");
                    _instance = new GameObject("VFXManager").AddComponent<VFXManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Line Clear Sounds (Assign in Inspector)")]
    public AudioClip clear1LineSFX;
    public AudioClip clear2LinesSFX;
    public AudioClip clear3LinesSFX;
    public AudioClip clear4LinesSFX;

    [Header("Line Clear Particle Prefabs (Assign in Inspector)")]
    public GameObject clear1LineFXPrefab;
    public GameObject clear2LinesFXPrefab;
    public GameObject clear3LinesFXPrefab;
    public GameObject clear4LinesFXPrefab;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D Sound
    }

    public void ShowLineClearText(int lineCount, Vector3 position)
    {
        string text = "";
        Color color = Color.white;
        float fontScale = 14f;
        AudioClip soundToPlay = null;

        switch (lineCount)
        {
            case 1:
                text = "GOOD!";
                color = new Color(0.2f, 0.8f, 1f);
                fontScale = 12f;
                soundToPlay = clear1LineSFX;
                break;
            case 2:
                text = "AMAZING!!";
                color = new Color(0.2f, 1f, 0.2f);
                fontScale = 14f;
                soundToPlay = clear2LinesSFX;
                break;
            case 3:
                text = "WELL DONE!!!";
                color = new Color(1f, 0.6f, 0f);
                fontScale = 16f;
                soundToPlay = clear3LinesSFX;
                break;
            case 4:
                text = "UNBELIEVABLE!!!!";
                color = new Color(1f, 0.2f, 0.8f);
                fontScale = 18f;
                soundToPlay = clear4LinesSFX;
                break;
            default: return; // 0 veya 4'ten fazla satýr için iþlem yapma
        }

        // Ses efekti satýr sayýsý kadar çalýnsýn
        if (soundToPlay != null && _audioSource != null)
        {
            for (int i = 0; i < lineCount; i++)
            {
                _audioSource.PlayOneShot(soundToPlay);
            }
        }

        // Görsel efekt (TextMeshPro)
        GameObject textObj = new GameObject("LineClearText");
        textObj.transform.position = position;

        TextMeshPro tmpro = textObj.AddComponent<TextMeshPro>();
        tmpro.text = text;
        tmpro.color = color;
        tmpro.fontSize = fontScale;
        tmpro.alignment = TextAlignmentOptions.Center;
        tmpro.fontStyle = FontStyles.Bold;
        tmpro.sortingOrder = 100;

        StartCoroutine(AnimateFloatingText(tmpro));
    }

    private IEnumerator AnimateFloatingText(TextMeshPro textMesh)
    {
        float duration = 1.2f;
        float elapsed = 0f;
        Vector3 startPos = textMesh.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;
        Vector3 startScale = Vector3.zero;
        Vector3 peakScale = Vector3.one * 1.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Move up
            textMesh.transform.position = Vector3.Lerp(startPos, endPos, t);

            // Pop scale
            if (t < 0.2f)
            {
                textMesh.transform.localScale = Vector3.Lerp(startScale, peakScale, t / 0.2f);
            }
            else
            {
                textMesh.transform.localScale = Vector3.Lerp(peakScale, Vector3.one, (t - 0.2f) / 0.8f);
            }

            // Fade out at end
            if (t > 0.6f)
            {
                Color c = textMesh.color;
                c.a = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                textMesh.color = c;
            }

            yield return null;
        }

        Destroy(textMesh.gameObject);
    }

    public void PlayClearParticles(List<Transform> blocks, int lineCount)
    {
        GameObject customPrefab = null;

        // Select the custom particle prefab based on the line count
        switch (lineCount)
        {
            case 1: customPrefab = clear1LineFXPrefab; break;
            case 2: customPrefab = clear2LinesFXPrefab; break;
            case 3: customPrefab = clear3LinesFXPrefab; break;
            case 4: customPrefab = clear4LinesFXPrefab; break;
        }

        foreach (var block in blocks)
        {
            if (block == null) continue;

            if (customPrefab != null)
            {
                // Instantiate the user's custom particle prefab
                GameObject pObj = Instantiate(customPrefab, block.position, Quaternion.identity);
                Destroy(pObj, 3f); // Clean up after 3 seconds assuming particles have finished
            }
            else
            {
                // Fallback dynamically generated simple particle effect
                GameObject pObj = new GameObject("ClearParticle");
                pObj.transform.position = block.position;
                
                ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.duration = 0.5f;
                main.startLifetime = 0.4f;
                main.startSpeed = 4f;
                main.startSize = 0.3f;
                main.loop = false;
                
                SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    main.startColor = sr.color;
                }

                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.4f;

                ParticleSystemRenderer psr = pObj.GetComponent<ParticleSystemRenderer>();
                psr.material = new Material(Shader.Find("Sprites/Default"));
                psr.sortingOrder = 50;

                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                AnimationCurve curve = new AnimationCurve();
                curve.AddKey(0f, 1f);
                curve.AddKey(1f, 0f);
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

                ps.Play();
                Destroy(pObj, 1f);
            }
        }
    }
}
