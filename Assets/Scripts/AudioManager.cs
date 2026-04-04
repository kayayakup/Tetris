using UnityEngine;

/// <summary>
/// Manages all game audio, including BGM and SFX with individual volume controls.
/// Registers itself with the GameManager for easy access from other scripts.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Background Music")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("SFX Clips")]
    public AudioClip lockClip;
    [Range(0f, 1f)] public float lockVolume = 0.7f;
    
    public AudioClip clearClip;
    [Range(0f, 1f)] public float clearVolume = 0.8f;

    public AudioClip rotateClip;
    [Range(0f, 1f)] public float rotateVolume = 0.4f;

    public AudioClip moveClip;
    [Range(0f, 1f)] public float moveVolume = 0.3f;

    public AudioClip gameOverClip;
    [Range(0f, 1f)] public float gameOverVolume = 0.9f;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    private void Awake()
    {
        // 1. Setup Audio Sources
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.spatialBlend = 0f; // Force 2D

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f; // Force 2D
        _sfxSource.volume       = 1f; // Ensure base volume is 1

        // 3. Start BGM if available
        if (bgmClip != null)
        {
            _bgmSource.clip = bgmClip;
            _bgmSource.volume = bgmVolume;
            _bgmSource.Play();
        }
    }

    // ─── Public SFX API (Null-checked) ──────────────────────────────────────────

    public void PlayLock()     => PlaySFX(lockClip, lockVolume);
    public void PlayClear()    => PlaySFX(clearClip, clearVolume);
    public void PlayRotate()   => PlaySFX(rotateClip, rotateVolume);
    public void PlayMove()     => PlaySFX(moveClip, moveVolume);
    public void PlayGameOver() => PlaySFX(gameOverClip, gameOverVolume);

    private void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (_sfxSource == null)
        {
            Debug.LogError($"[AudioManager] _sfxSource is null! Clip: {clip.name}");
            return;
        }
        
        Debug.Log($"[AudioManager] Playing SFX: {clip.name} (Volume: {volume})");
        _sfxSource.PlayOneShot(clip, volume);
    }

    // Call this if BGM volume is changed via inspector at runtime
    private void OnValidate()
    {
        if (_bgmSource != null) _bgmSource.volume = bgmVolume;
    }
}
