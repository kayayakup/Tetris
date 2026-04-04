using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Swipe-based input using Unity New Input System's EnhancedTouch API.
/// Swipe Right  → MoveRight
/// Swipe Left   → MoveLeft
/// Swipe Up     → Rotate
/// Swipe Down   → HardDrop
/// </summary>
public class InputHandler : MonoBehaviour
{
    // ─── Thresholds ───────────────────────────────────────────────────────────
    [Header("Swipe Settings")]
    [Tooltip("Minimum swipe distance in pixels to register a swipe.")]
    public float minSwipeDistance = 50f;

    [Tooltip("Maximum time in seconds for a gesture to count as a swipe.")]
    public float maxSwipeTime = 0.4f;

    [Header("Hold Settings")]
    [Tooltip("Time in seconds before a touch registers as a hold.")]
    public float holdThreshold = 0.15f;
    [Tooltip("Interval between soft drops while holding.")]
    public float softDropInterval = 0.05f;

    // ─── State ────────────────────────────────────────────────────────────────
    private Tetromino _activePiece;

    private Vector2 _startPos;
    private float   _startTime;
    private bool    _trackingSwipe;
    private bool    _isHolding;
    private float   _dropTimer;

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_trackingSwipe && _activePiece != null && !GameManager.Instance.IsGameOver && !GameManager.Instance.IsPaused)
        {
            float elapsed = Time.time - _startTime;
            if (elapsed > holdThreshold)
            {
                // Check current finger position
                if (Touch.activeFingers.Count > 0 && Touch.activeFingers[0].index == 0)
                {
                    Vector2 currentPos = Touch.activeFingers[0].currentTouch.screenPosition;
                    if (Vector2.Distance(currentPos, _startPos) < minSwipeDistance)
                    {
                        _isHolding = true;
                        
                        _dropTimer += Time.deltaTime;
                        if (_dropTimer >= softDropInterval)
                        {
                            _dropTimer = 0f;
                            _activePiece.SoftDrop();
                        }
                    }
                    else
                    {
                        _isHolding = false; // Moved too much → not a hold anymore (it's a swipe)
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnFingerDown;
        Touch.onFingerUp   += OnFingerUp;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerUp   -= OnFingerUp;
        EnhancedTouchSupport.Disable();
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    public void SetActivePiece(Tetromino piece)
    {
        _activePiece = piece;
    }

    // ─── Touch callbacks ──────────────────────────────────────────────────────
    private void OnFingerDown(Finger finger)
    {
        // Only track the first finger
        if (finger.index != 0) return;

        _startPos      = finger.currentTouch.startScreenPosition;
        _startTime     = Time.time;
        _trackingSwipe = true;
        _isHolding     = false;
        _dropTimer     = 0f;
    }

    private void OnFingerUp(Finger finger)
    {
        if (!_trackingSwipe || finger.index != 0) return;
        _trackingSwipe = false;
        _isHolding     = false;

        if (_activePiece == null) return;
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        float elapsed = Time.time - _startTime;
        if (elapsed > maxSwipeTime) return; // too slow → not a swipe

        Vector2 endPos = finger.currentTouch.screenPosition;
        Vector2 delta  = endPos - _startPos;
        float   dist   = delta.magnitude;

        if (dist < minSwipeDistance) return; // too short → not a swipe

        // Determine dominant direction
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            // Horizontal swipe
            if (delta.x > 0)
                _activePiece.MoveRight();
            else
                _activePiece.MoveLeft();
        }
        else
        {
            // Vertical swipe
            if (delta.y > 0)
                _activePiece.Rotate();
            else
                _activePiece.HardDrop();
        }
    }
}
