using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class NextPieceDisplay : MonoBehaviour
{
    private Image[] _blocks = new Image[4];
    
    [Tooltip("Size of each grid cell block inside the Next Piece UI")]
    public float pieceBlockSize = 100f; 

    private void Awake()
    {
        // Disable base background image if one exists, as we will draw blocks individually
        Image bg = GetComponent<Image>();
        if (bg != null) bg.enabled = false;

        // Generate the 4 individual blocks as children
        for (int i = 0; i < 4; i++)
        {
            GameObject blockObj = new GameObject("Block_" + i);
            blockObj.transform.SetParent(transform, false);
            
            Image img = blockObj.AddComponent<Image>();
            _blocks[i] = img;

            RectTransform rt = blockObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(pieceBlockSize, pieceBlockSize);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.Spawner != null)
        {
            GameManager.Instance.Spawner.OnNextPieceChanged.AddListener(UpdateNextPiece);
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
                int[,] shapeCoords = Tetromino.Shapes[index];

                for (int i = 0; i < 4; i++)
                {
                    if (_blocks[i] != null)
                    {
                        _blocks[i].sprite = s;
                        _blocks[i].enabled = true;
                        
                        int col = shapeCoords[i, 0];
                        int row = shapeCoords[i, 1];
                        
                        // Set standard tetris block UI positioning relative to the center
                        _blocks[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(col * pieceBlockSize, row * pieceBlockSize);
                    }
                }
            }
        }
    }
}
