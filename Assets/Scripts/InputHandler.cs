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
    [Tooltip("Minimum swipe distance in pixels to register a swipe or step.")]
    public float minSwipeDistance = 1075f;

    [Tooltip("Maximum time in seconds for a gesture to count as a swipe or tap.")]
    public float maxSwipeTime = 0.4f;

    [Tooltip("Maximum distance in pixels to count as a tap for rotation.")]
    public float maxTapDistance = 40f;

    // ─── State ────────────────────────────────────────────────────────────────
    private Tetromino _activePiece;
    private Vector2 _startPos;
    private float   _startTime;
    private bool    _trackingSwipe;
    private float   _lastHorizontalMoveX; // For continuous drag tracking
    private float   _lastVerticalMoveY;   // For continuous drop tracking

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_trackingSwipe && _activePiece != null && !GameManager.Instance.IsGameOver && !GameManager.Instance.IsPaused)
        {
            if (Touch.activeFingers.Count > 0 && Touch.activeFingers[0].index == 0)
            {
                Vector2 currentPos = Touch.activeFingers[0].currentTouch.screenPosition;
                
                // Continuous horizontal drag
                float deltaX = currentPos.x - _lastHorizontalMoveX;
                if (Mathf.Abs(deltaX) >= minSwipeDistance)
                {
                    int steps = Mathf.FloorToInt(Mathf.Abs(deltaX) / minSwipeDistance);
                    int sign = (int)Mathf.Sign(deltaX);
                    for (int i = 0; i < steps; i++)
                    {
                        if (sign > 0) _activePiece.MoveRight();
                        else _activePiece.MoveLeft();
                    }
                    _lastHorizontalMoveX += steps * sign * minSwipeDistance;
                }

                // Continuous vertical drag (downwards only)
                float deltaY = currentPos.y - _lastVerticalMoveY;
                if (deltaY <= -minSwipeDistance)
                {
                    int steps = Mathf.FloorToInt(Mathf.Abs(deltaY) / minSwipeDistance);
                    for (int i = 0; i < steps; i++)
                    {
                        _activePiece.SoftDrop();
                    }
                    // Subtract to track downwards movement properly
                    _lastVerticalMoveY -= steps * minSwipeDistance;
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
        _trackingSwipe = false; // Reset tracking immediately. The user must lift and touch again for the new piece.
    }

    // ─── Touch callbacks ──────────────────────────────────────────────────────
    private void OnFingerDown(Finger finger)
    {
        // Only track the first finger
        if (finger.index != 0) return;

        _startPos      = finger.currentTouch.startScreenPosition;
        _lastHorizontalMoveX = _startPos.x;
        _lastVerticalMoveY   = _startPos.y;
        _startTime     = Time.time;
        _trackingSwipe = true;
    }

    private void OnFingerUp(Finger finger)
    {
        if (!_trackingSwipe || finger.index != 0) return;
        _trackingSwipe = false;

        if (_activePiece == null) return;
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        float elapsed = Time.time - _startTime;
        Vector2 endPos = finger.currentTouch.screenPosition;
        Vector2 delta  = endPos - _startPos;
        float   dist   = delta.magnitude;

        // Condition for TAP (Rotate)
        if (dist <= maxTapDistance && elapsed <= maxSwipeTime)
        {
            _activePiece.Rotate();
            return;
        }

        // Condition for Fast Vertical Swipe (Hard Drop)
        if (dist >= minSwipeDistance && elapsed <= maxSwipeTime)
        {
            if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) && delta.y < 0)
            {
                _activePiece.HardDrop();
            }
        }
    }
}
