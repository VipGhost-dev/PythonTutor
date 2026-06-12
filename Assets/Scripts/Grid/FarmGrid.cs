using UnityEngine;
using System.Collections.Generic;

public class FarmGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 15;
    public int height = 15;
    public float cellSize = 1f;
    public GameObject cellPrefab;
    
    [Header("Generation Settings")]
    public int seed = -1;  // -1 = случайный seed
    public float stoneDensity = 0.05f;
    public float grassDensity = 0.06f;
    public int minSoilArea = 15;
    
    [Header("Visuals")]
    public Material soilMaterial;
    public Material grassMaterial;
    public Material stoneMaterial;

    [Header("Crop Prefabs")]
    public GameObject wheatPrefab;
    public GameObject cornPrefab;
    public GameObject carrotPrefab;
    
    private Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
    private Dictionary<Vector2Int, CropData> crops = new Dictionary<Vector2Int, CropData>();
    private System.Random random;
    
    private class CropData
    {
        public string type;
        public float plantTime;
        public GameObject visual;
    }
    
    void Start()
    {
        GenerateNewField();
    }
    
    public void GenerateNewField()
    {
        if (seed == -1)
            seed = System.Environment.TickCount;
        
        random = new System.Random(seed);
        Debug.Log($"Generating field with seed: {seed}");
        
        GenerateGrid();
    }
    
    void GenerateGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        cells.Clear();
        crops.Clear();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Vector3 worldPos = new Vector3(x * cellSize, 0, y * cellSize);
                
                GameObject cellObj = Instantiate(cellPrefab, worldPos, Quaternion.identity, transform);
                cellObj.name = $"Cell_{x}_{y}";
                
                Cell cell = cellObj.GetComponent<Cell>();
                if (cell == null)
                    cell = cellObj.AddComponent<Cell>();
                
                cell.gridPosition = pos;
                cells[pos] = cell;
            }
        }
        
        GenerateTerrain();
        
        Debug.Log($"Grid generated: {width}x{height} with seed {seed}");
    }
    
    void GenerateTerrain()
    {
        foreach (var cell in cells)
        {
            cell.Value.SetType(Cell.CellType.Soil);
        }
        
        for (int x = 0; x < width; x++)
        {
            cells[new Vector2Int(x, 0)].SetType(Cell.CellType.Grass);      
            cells[new Vector2Int(x, height - 1)].SetType(Cell.CellType.Grass); 
        }
        
        for (int y = 0; y < height; y++)
        {
            cells[new Vector2Int(0, y)].SetType(Cell.CellType.Grass);      
            cells[new Vector2Int(width - 1, y)].SetType(Cell.CellType.Grass); 
        }
        
        int stoneCount = (int)(width * height * stoneDensity);
        for (int i = 0; i < stoneCount; i++)
        {
            int x = random.Next(1, width - 1);
            int y = random.Next(1, height - 1);
            Vector2Int pos = new Vector2Int(x, y);
            
            if (cells[pos].type == Cell.CellType.Soil)
            {
                cells[pos].SetType(Cell.CellType.Stone);
            }
        }
        
        int grassCount = (int)(width * height * grassDensity);
        for (int i = 0; i < grassCount; i++)
        {
            int x = random.Next(1, width - 1);
            int y = random.Next(1, height - 1);
            Vector2Int pos = new Vector2Int(x, y);
            
            if (cells[pos].type == Cell.CellType.Soil)
            {
                cells[pos].SetType(Cell.CellType.Grass);
            }
        }
        
        int lakeCount = random.Next(1, 4);
        for (int i = 0; i < lakeCount; i++)
        {
            int centerX = random.Next(3, width - 3);
            int centerY = random.Next(3, height - 3);
            int radius = random.Next(2, 5);
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        Vector2Int pos = new Vector2Int(centerX + x, centerY + y);
                        if (cells.ContainsKey(pos) && cells[pos].type == Cell.CellType.Soil)
                        {
                            if (random.NextDouble() < 0.7f)
                                cells[pos].SetType(Cell.CellType.Grass);
                        }
                    }
                }
            }
        }
    }
    
    public void RegenerateField()
    {
        seed = System.Environment.TickCount;
        random = new System.Random(seed);
        GenerateGrid();
    }
    
    public void RegenerateFieldWithSeed(int newSeed)
    {
        seed = newSeed;
        random = new System.Random(seed);
        GenerateGrid();
    }
    
    public bool IsWalkable(Vector2Int position)
    {
        if (!cells.ContainsKey(position)) return false;
        return cells[position].isWalkable && !crops.ContainsKey(position);
    }
    
    public bool CanPlantAtPosition(Vector2Int position)
    {
        if (!cells.ContainsKey(position)) return false;
        return cells[position].type == Cell.CellType.Soil && !crops.ContainsKey(position);
    }
    
    public void PlantAtPosition(Vector2Int position, string seedType)
    {
        if (!CanPlantAtPosition(position)) return;
        
        GameObject plant = CreatePlantVisual(seedType);
        
        CropData crop = new CropData
        {
            type = seedType,
            plantTime = Time.time,
            visual = plant
        };
        
        crops[position] = crop;
        cells[position].SetCrop(plant);
        
        Debug.Log($"Planted {seedType} at {position}");
    }
    
    GameObject CreatePlantVisual(string seedType)
    {
        switch (seedType)
        {
            case "wheat":
                return Instantiate(wheatPrefab);

            case "corn":
                return Instantiate(cornPrefab);

            case "carrot":
                return Instantiate(carrotPrefab);

            default:
                return null;
        }
    }
    
    public bool HasCrop(Vector2Int position)
    {
        return crops.ContainsKey(position);
    }
    
    public Vector2Int GetRandomSoilPosition()
    {
        List<Vector2Int> soilPositions = new List<Vector2Int>();
        
        foreach (var cell in cells)
        {
            if (cell.Value.type == Cell.CellType.Soil && !crops.ContainsKey(cell.Key))
            {
                soilPositions.Add(cell.Key);
            }
        }
        
        if (soilPositions.Count > 0)
            return soilPositions[random.Next(soilPositions.Count)];
        
        return new Vector2Int(width / 2, height / 2);
    }
    
    public int GetSoilCount()
    {
        int count = 0;
        foreach (var cell in cells)
        {
            if (cell.Value.type == Cell.CellType.Soil)
                count++;
        }
        return count;
    }
    
    public string GetFieldInfo()
    {
        int soil = 0, grass = 0, stone = 0;
        foreach (var cell in cells)
        {
            switch (cell.Value.type)
            {
                case Cell.CellType.Soil: soil++; break;
                case Cell.CellType.Grass: grass++; break;
                case Cell.CellType.Stone: stone++; break;
            }
        }
        return $"Seed: {seed}\nSoil: {soil}, Grass: {grass}, Stones: {stone}\nCrops: {crops.Count}";
    }

    public bool HasCropOfType(Vector2Int position, string cropType)
    {
        if (!crops.ContainsKey(position))
            return false;

        return crops[position].type == cropType;
    }

    public bool IsSoil(Vector2Int position)
    {
        if (!cells.ContainsKey(position)) return false;
        return cells[position].type == Cell.CellType.Soil;
    }
}