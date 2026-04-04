using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Static helper that plays a middle-out smooth fade animation on cleared row blocks.
/// Called by GridSystem before destroying the blocks.
/// </summary>
public class LineClearAnimation : MonoBehaviour
{
    [Header("Animation Timings")]
    public static float MiddleOutDelay = 0.04f;   // Delay step from center
    public static float FadeDuration  = 0.25f;   // Scale/fade duration

    /// <summary>
    /// Plays the full line-clear animation on the given list of transforms
    /// </summary>
    public static IEnumerator PlayAll(List<Transform> blocks)
    {
        if (blocks.Count == 0 || GameManager.Instance == null) yield break;

        // Center calculation
        float centerX = GameManager.Instance.gridWidth / 2f;
        
        Dictionary<Transform, float> distMap = new Dictionary<Transform, float>();
        foreach (var b in blocks)
        {
            if (b == null) continue;
            Vector2Int gridPos = GameManager.Instance.GridSystem.WorldToGrid(b.position);
            distMap[b] = Mathf.Abs(gridPos.x - centerX);
        }

        // Group blocks by distance
        var groupedBlocks = blocks.Where(b => b != null)
                                  .OrderBy(b => distMap[b])
                                  .GroupBy(b => distMap[b])
                                  .ToList();

        float delay = 0f;

        // Use GameManager as the MonoBehaviour context to spawn concurrent sequences
        foreach (var group in groupedBlocks)
        {
            List<SpriteRenderer> renderers = new List<SpriteRenderer>();
            foreach (Transform b in group)
            {
                SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
                if (sr != null) renderers.Add(sr);
            }

            GameManager.Instance.StartCoroutine(MiddleOutSequence(renderers, delay));
            delay += MiddleOutDelay;
        }

        // Wait for all sequences to finish
        yield return new WaitForSeconds(delay + FadeDuration);
    }

    private static IEnumerator MiddleOutSequence(List<SpriteRenderer> renderers, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        
        Dictionary<SpriteRenderer, Vector3> initialScales = new Dictionary<SpriteRenderer, Vector3>();
        foreach (var sr in renderers)
        {
            if (sr != null) initialScales[sr] = sr.transform.localScale;
        }

        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FadeDuration;
            
            float alpha = Mathf.Lerp(1f, 0f, t);
            float scaleMult = Mathf.Lerp(1f, 0f, t);

            foreach (var sr in renderers)
            {
                if (sr == null) continue;
                
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;

                sr.transform.localScale = initialScales[sr] * scaleMult;
            }

            yield return null;
        }

        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
            sr.transform.localScale = Vector3.zero;
        }
    }
}
