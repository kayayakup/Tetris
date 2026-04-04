using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static helper that plays a flash → fade-out animation on cleared row blocks.
/// Called by GridSystem before destroying the blocks.
/// </summary>
public class LineClearAnimation : MonoBehaviour
{
    // ─── Animation parameters ─────────────────────────────────────────────────
    [Header("Animation Timings")]
    public static float FlashDuration = 0.08f;   // per flash cycle
    public static int   FlashCount    = 3;        // number of flashes
    public static float FadeDuration  = 0.18f;   // final fade-out

    /// <summary>
    /// Plays the full line-clear animation on the given list of transforms
    /// (each must have a SpriteRenderer). Returns when animation is complete.
    /// </summary>
    public static IEnumerator PlayAll(List<Transform> blocks)
    {
        // Collect sprite renderers
        List<SpriteRenderer> renderers = new List<SpriteRenderer>(blocks.Count);
        foreach (var b in blocks)
        {
            if (b == null) continue;
            SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
            if (sr != null) renderers.Add(sr);
        }

        // ── Phase 1: Flash ────────────────────────────────────────────────────
        for (int f = 0; f < FlashCount; f++)
        {
            SetAlpha(renderers, 1f);
            yield return new WaitForSeconds(FlashDuration);
            SetAlpha(renderers, 0f);
            yield return new WaitForSeconds(FlashDuration);
        }

        // ── Phase 2: Fade out ─────────────────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / FadeDuration);
            SetAlpha(renderers, alpha);
            yield return null;
        }

        SetAlpha(renderers, 0f);
    }

    private static void SetAlpha(List<SpriteRenderer> renderers, float alpha)
    {
        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
