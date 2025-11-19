using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Cell : MonoBehaviour, IPointerClickHandler
{
    public bool isBomb;
    public bool isTriggered;
    public bool isFlagged;

    public GridGenerator.Coord coord;
    [SerializeField] private TMP_Text txt;
    [SerializeField] private Image img;

    public void Initialize(GridGenerator.Coord coord, bool isBomb)
    {
        this.coord = coord;
        this.isBomb = isBomb;
        gameObject.name = $"{coord.x}, {coord.y}";
    }

    public void Trigger() {
        if (!GameManager.instance.gameStarted)
        {
            GridGenerator.instance.Generate(coord);
            return;
        }
        if (isTriggered || isFlagged || GameManager.instance.gameEnded) return;
        isTriggered = true;
        if (isBomb)
        {
            img.color = Color.red;
            GameManager.instance.LoseGame();
        } else
        {
            int surroundingBombs = getSurroundingBombCount();
            if (surroundingBombs == 0)
            {
                img.color = Color.blue;
                for (int x = coord.x - 1; x <= coord.x + 1; x++)
                {
                    for (int y = coord.y - 1; y <= coord.y + 1; y++)
                    {
                        if (x >= GridGenerator.instance.MAP_WIDTH || x < 0 || y >= GridGenerator.instance.MAP_HEIGHT || y < 0) continue;
                        Cell cell = GridGenerator.instance.getCellAtPosition(x, y);
                        if (cell == null)
                        {
                            Debug.LogWarning($"No cell found at {x}, {y}");
                            continue;
                        }
                        if (!cell.isBomb) cell.Trigger();
                    }
                }
            } else
            {
                img.color = Color.lightGray;
                txt.text = surroundingBombs.ToString();
                txt.gameObject.SetActive(true);
            }

            GameManager.instance.CheckCells();
        }
    }

    public void ToggleFlag(bool toggle)
    {
        if (isTriggered || GameManager.instance.gameEnded || !GameManager.instance.gameStarted) return;
        isFlagged = toggle;
        if (isFlagged)
        {
            img.color = Color.hotPink;
        } else
        {
            img.color = Color.white;
        }
    }

    int getSurroundingBombCount()
    {
        int surroundingBombs = 0;
        for (int x = coord.x - 1; x <= coord.x + 1; x++)
        {
            for (int y = coord.y - 1; y <= coord.y + 1; y++)
            {
                if (x >= GridGenerator.instance.MAP_WIDTH || x < 0 || y >= GridGenerator.instance.MAP_HEIGHT || y < 0) continue;
                if (GridGenerator.instance.map[x, y] == 1)
                {
                    surroundingBombs++;
                }
            }
        }
        return surroundingBombs;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Trigger();
        } else if (eventData.button == PointerEventData.InputButton.Right)
        {
            ToggleFlag(!isFlagged);
        }
    }
}
