using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Cell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool isBomb;
    public bool isTriggered;
    public bool isFlagged;

    public GridGenerator.Coord coord;
    [SerializeField] private TMP_Text txt;
    [SerializeField] private Image icon, bg;
    [SerializeField] private Sprite bombSprite, xSprite, flagSprite, safeSprite;

    public string curImg;

    public Color baseColor1, baseColor2, baseHoverColor, pickedColor1, pickedColor2, pickedHoverColor = Color.white;
    public Color[] possibleBombColors, possibleBombInnerColors;

    Color defaultColor;
    public bool hovering = false;
    public bool chording = false;

    private bool leftClickHeld, rightClickHeld;

    private InputSystem_Actions inputActions;

    void OnEnable()
    {
        if (inputActions != null) inputActions.Enable();
    }

    void OnDisable()
    {
        if (inputActions != null) inputActions.Disable();
    }

    void Start()
    {
        inputActions = new InputSystem_Actions();

        inputActions.Player.Attack.performed += delegate
        {
            leftClickHeld = true;
            if (rightClickHeld)
            {
                chording = true;
                CheckChordSurroundings();
            }
        };
        inputActions.Player.Attack.canceled += delegate
        {
            leftClickHeld = false;
            chording = false;
        };

        inputActions.Player.ADS.performed += delegate
        {
            rightClickHeld = true;
            if (leftClickHeld)
            {
                chording = true;
                CheckChordSurroundings();
            }
        };
        inputActions.Player.ADS.canceled += delegate
        {
            rightClickHeld = false;
            chording = false;
        };

        inputActions.Enable();
    }

    public void Initialize(GridGenerator.Coord coord, bool isBomb)
    {
        this.coord = coord;
        this.isBomb = isBomb;
        gameObject.name = $"{coord.x}, {coord.y}";

        if (coord.y % 2 == 0)
        {
            if (coord.x % 2 == 0)
            {
                bg.color = baseColor1;
                defaultColor = baseColor1;
            } else
            {
                bg.color = baseColor2;
                defaultColor = baseColor2;
            }
        } else
        {
            if (coord.x % 2 != 0)
            {
                bg.color = baseColor1;
                defaultColor = baseColor1;
            } else
            {
                bg.color = baseColor2;
                defaultColor = baseColor2;
            }
        }
    }

    public void Trigger() {
        if (isTriggered || isFlagged || GameManager.instance.gameEnded.Value || GameManager.instance.hasLost) return;
        isTriggered = true;
        if (isBomb)
        {
            SetImage("bomb");

            GameManager.instance.LoseGame();
        } else
        {
            int surroundingBombs = getSurroundingBombCount();
            SetTextColor();
            icon.color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
            if (surroundingBombs == 0)
            {
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
                txt.text = surroundingBombs.ToString();
                txt.gameObject.SetActive(true);
            }

            if (coord.y % 2 == 0)
            {
                if (coord.x % 2 == 0)
                {
                    bg.color = pickedColor1;
                    defaultColor = pickedColor1;
                } else
                {
                    bg.color = pickedColor2;
                    defaultColor = pickedColor2;
                }
            } else
            {
                if (coord.x % 2 != 0)
                {
                    bg.color = pickedColor1;
                    defaultColor = pickedColor1;
                } else
                {
                    bg.color = pickedColor2;
                    defaultColor = pickedColor2;
                }
            }
            if (hovering) Hover();

            GameManager.instance.CheckCells();
        }
    }

    void Update()
    {
        if (chording && !isFlagged && !isTriggered)
        {
            bool foundChording = false;
            List<Cell> flaggedSurrounding = new List<Cell>();
            List<Cell> surrounding = new List<Cell>();
            for (int x = coord.x - 1; x <= coord.x + 1; x++)
            {
                for (int y = coord.y - 1; y <= coord.y + 1; y++)
                {
                    Cell cell = GridGenerator.instance.getCellAtPosition(x, y);
                    if (cell)
                    {
                        if (cell.hovering) foundChording = true;
                        if (cell.isFlagged) flaggedSurrounding.Add(cell);
                        surrounding.Add(cell);
                    }
                }
            }

            if (foundChording)
            {
                if (isBomb)
                {
                    foreach (Cell flagged in flaggedSurrounding)
                    {
                        if (!flagged.isBomb)
                            Trigger();
                    }
                }
                HoverFX();
            }
            else if (!hovering) UnhoverFX();
        } else if (!hovering) UnhoverFX();
    }

    public void ToggleFlag(bool toggle)
    {
        if (isTriggered || GameManager.instance.gameEnded.Value || !GameManager.instance.gameStarted.Value) return;
        isFlagged = toggle;
        if (isFlagged)
        {
            SetImage("flag");
        } else
        {
            SetImage("");
        }
        GameManager.instance.CheckCells();
    }

    public int getSurroundingBombCount()
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

    public void SetImage(string name)
    {
        switch (name)
        {
            case "bomb":
                icon.sprite = bombSprite;
                int bombColorIndex = Random.Range(0, possibleBombColors.Length);
                bg.color = possibleBombColors[bombColorIndex];
                icon.color = possibleBombInnerColors[bombColorIndex];
                break;
            case "x":
                icon.sprite = xSprite;
                break;
            case "flag":
                icon.sprite = flagSprite;
                break;
            case "safe":
                icon.sprite = safeSprite;
                break;
            default:
                icon.sprite = null;
                bg.color = defaultColor;
                break;
        }
        if (icon.sprite)
        {
            curImg = name;
            icon.gameObject.SetActive(true);
        }
        else
        {
            curImg = "";
            icon.gameObject.SetActive(false);
        }
    }

    public void SetTextColor()
    {
        switch (getSurroundingBombCount())
        {
            case 1:
                txt.color = new Color(0f/255f, 119f/255f, 211f/255f);
                break;
            case 2:
                txt.color = new Color(50f/255f, 148f/255f, 68f/255f);
                break;
            case 3:
                txt.color = new Color(209f/255f, 41f/255f, 47f/255f);
                break;
            case 4:
                txt.color = new Color(119f/255f, 37f/255f, 164f/255f);
                break;
            case 5:
                txt.color = new Color(237f/255f, 145f/255f, 34f/255f);
                break;
            case 6:
                txt.color = new Color(0f/255f, 151f/255f, 169f/255f);
                break;
            case 7:
                txt.color = new Color(63f/255f, 64f/255f, 68f/255f);
                break;
            case 8:
                txt.color = new Color(168f/255f, 158f/255f, 148f/255f);
                break;
        }
    }
    
    public void Hover()
    {
        if ((isTriggered && isBomb) || GameManager.instance.hasLost || GameManager.instance.gameEnded.Value) return;
        hovering = true;
        HoverFX();
    }

    public void Unhover()
    {
        hovering = false;
        if ((isTriggered && isBomb) || GameManager.instance.hasLost || GameManager.instance.gameEnded.Value) return;
        UnhoverFX();
    }

    public void HoverFX()
    {
        if ((isTriggered && isBomb) || GameManager.instance.hasLost || GameManager.instance.gameEnded.Value) return;
        if (isTriggered)
        {
            if (getSurroundingBombCount() != 0) bg.color = pickedHoverColor;
            else bg.color = defaultColor;
        } else
        {
            bg.color = baseHoverColor;
        }
    }

    public void UnhoverFX()
    {
        if ((isTriggered && isBomb) || GameManager.instance.hasLost || GameManager.instance.gameEnded.Value) return;
        bg.color = defaultColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Unhover();
    }

    void CheckChordSurroundings()
    {
        if (chording && !isFlagged && isTriggered && hovering)
        {
            List<Cell> flaggedSurrounding = new List<Cell>();
            List<Cell> surrounding = new List<Cell>();

            for (int x = coord.x - 1; x <= coord.x + 1; x++)
            {
                for (int y = coord.y - 1; y <= coord.y + 1; y++)
                {
                    Cell cell = GridGenerator.instance.getCellAtPosition(x, y);
                    if (cell)
                    {
                        if (cell.isFlagged) flaggedSurrounding.Add(cell);
                        surrounding.Add(cell);
                    }
                }
            }

            
            int flaggedBombs = 0;
            foreach (Cell flagged in flaggedSurrounding) if (flagged.isBomb) flaggedBombs++;
            if (flaggedBombs == getSurroundingBombCount())
            {
                foreach (Cell cell in surrounding)
                {
                    if (!cell.isFlagged) cell.Trigger();
                }
            }
        }
    }
}
