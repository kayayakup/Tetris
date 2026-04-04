using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class NextPieceDisplay : MonoBehaviour
{
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.Spawner != null)
        {
            GameManager.Instance.Spawner.OnNextPieceChanged.AddListener(UpdateNextPiece);
            // initialize display if not null
            UpdateNextPiece(GameManager.Instance.Spawner.NextPieceType);
        }
    }

    private void UpdateNextPiece(Tetromino.PieceType nextType)
    {
        if (GameManager.Instance != null && GameManager.Instance.tetrominoSprites != null)
        {
            int index = (int)nextType;
            if (index >= 0 && index < GameManager.Instance.tetrominoSprites.Length)
            {
                Sprite s = GameManager.Instance.tetrominoSprites[index];
                if (s != null)
                {
                    _image.sprite = s;
                    _image.enabled = true;
                }
            }
        }
    }
}
