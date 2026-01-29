using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public static GridGenerator instance {get; private set;}

    public long seed;
    public int[,] map;

    public int MAP_WIDTH = 10;
    public int MAP_HEIGHT = 10;

    public float bombPercentage = 12.5f;
    [HideInInspector] public int bombCount;
    
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform cellParent;
    public Cell[,] cells;

    public bool generated {get; private set;} = false;

    void Awake()
    {
        instance = this;

        MAP_WIDTH = GameOptionPersistence.gridX;
        MAP_HEIGHT = GameOptionPersistence.gridY;
        bombPercentage = GameOptionPersistence.bombPercentage;
    }

    void Start()
    {
        RectTransform holderTransform = cellParent.GetComponent<RectTransform>();
        holderTransform.sizeDelta = new Vector2(MAP_WIDTH * 100, MAP_HEIGHT * 100);
        holderTransform.localScale = new Vector3(10f/MAP_WIDTH, 10f/MAP_WIDTH, 1);

        bombCount = Mathf.RoundToInt(bombPercentage * (MAP_WIDTH * MAP_HEIGHT) / 100);
        GameManager.instance.UpdateFlagCount(bombCount);
    }

    public void Generate()
    {
        seed = GameManager.instance.seed.Value;

        if (cells != null && cells.Length > 0)
        {
            foreach (Cell cell in cells)
            {
                Destroy(cell.gameObject);
            }
        }
        
        map = new int[MAP_WIDTH, MAP_HEIGHT];
        cells = new Cell[MAP_WIDTH, MAP_HEIGHT]; 

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());
        Debug.Log($"Seed: {seed}");

        RectTransform holderTransform = cellParent.GetComponent<RectTransform>();
        holderTransform.sizeDelta = new Vector2(MAP_WIDTH * 100, MAP_HEIGHT * 100);
        holderTransform.localScale = new Vector3(10f/MAP_WIDTH, 10f/MAP_WIDTH, 1);

        if (GridZoom.instance)
        {
            GridZoom.instance.minScale = holderTransform.localScale.x;
            GridZoom.instance.ResetZoom();
        }

        bombCount = Mathf.RoundToInt(bombPercentage * (MAP_WIDTH * MAP_HEIGHT) / 100);
        Debug.Log($"Bomb count: {bombCount}");

        List<Coord> allCoords = new List<Coord>();

        for (int x = 0; x < MAP_WIDTH; x++)
            for (int y = 0; y < MAP_HEIGHT; y++)
                allCoords.Add(new Coord(x, y));
        
        allCoords = allCoords.OrderBy(_ => pseudoRandom.Next()).ToList();

        HashSet<Coord> bombCoords = new HashSet<Coord>(
            allCoords.Take(bombCount)
        );

        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT; y++)
            {
                Coord coord = new Coord(x, y);
                bool isBomb = bombCoords.Contains(coord);
                map[x, y] = isBomb ? 1 : 0;

                GameObject instantiatedCell = Instantiate(cellPrefab, cellParent);
                Cell cell = instantiatedCell.GetComponent<Cell>();
                cell.Initialize(coord, isBomb);
                cells[x, y] = cell;
            }
        }

        if (GameOptionPersistence.startPos)
        {
            Debug.Log("Getting start pos");
            List<Cell> zeroes = new List<Cell>();
            foreach (Cell cell in cells)
            {
                if (cell.getSurroundingBombCount() == 0) zeroes.Add(cell);
            }
            if (zeroes.Count != 0)
            {
                Cell startPos = zeroes[pseudoRandom.Next(0, zeroes.Count)];
                Debug.Log($"Got start pos: {startPos.coord.x}, {startPos.coord.y}");
                startPos.SetImage("safe");
            } else
            {
                List<Cell> safe = new List<Cell>();
                foreach (Cell cell in cells)
                {
                    if (!cell.isBomb) safe.Add(cell);
                }

                Cell startPos = safe[pseudoRandom.Next(0, safe.Count)];
                Debug.Log($"Got start pos: {startPos.coord.x}, {startPos.coord.y}");
                startPos.SetImage("safe");
            }
        }

        // Player.LocalPlayer.FlagsLeft.Value = bombCount;
        Player.LocalPlayer.BombsRemaining.Value = bombCount;

        generated = true;
    }

    public Cell getCellAtPosition(int x, int y)
    {
        if (x < 0 || y < 0 || x >= MAP_WIDTH || y >= MAP_HEIGHT)
            return null;
        return cells[x, y];
    }

    [System.Serializable]
    public class Coord
    {
        public int x;
        public int y;

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            
            Coord otherCoord = (Coord)obj;
            if (otherCoord.x == this.x && otherCoord.y == this.y) return true;

            return false;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(x, y);
        }
    }
}
