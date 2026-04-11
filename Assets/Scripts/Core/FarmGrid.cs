using UnityEngine;
using System.Collections.Generic;

public class FarmGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 15;
    public int height = 15;
    public GameObject cellPrefab;
    
    private Dictionary<Vector2Int, GameObject> cells = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, CropData> crops = new Dictionary<Vector2Int, CropData>();
    
    private class CropData
    {
        public string type;
        public float plantTime;
    }
    
    void Start()
    {
        GenerateGrid();
    }
    
    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GameObject cell = Instantiate(cellPrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                cell.name = $"Cell_{x}_{y}";
                
                // Простая логика: края - трава, внутри - земля
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    cell.GetComponent<Renderer>().material.color = Color.green;
                }
                else
                {
                    cell.GetComponent<Renderer>().material.color = new Color(0.6f, 0.4f, 0.2f); // Земля
                }
                
                cells[pos] = cell;
            }
        }
    }
    
    public bool IsWalkable(Vector2Int position)
    {
        if (!cells.ContainsKey(position)) return false;
        
        // Края - не ходим
        if (position.x == 0 || position.x == width - 1 || position.y == 0 || position.y == height - 1)
            return false;
        
        // Если есть урожай - не ходим
        return !crops.ContainsKey(position);
    }
    
    public bool CanPlantAtPosition(Vector2Int position)
    {
        return cells.ContainsKey(position) && 
               !crops.ContainsKey(position) &&
               position.x > 0 && position.x < width - 1 &&
               position.y > 0 && position.y < height - 1;
    }
    
    public void PlantAtPosition(Vector2Int position, string seedType)
    {
        if (!CanPlantAtPosition(position)) return;
        
        CropData crop = new CropData
        {
            type = seedType,
            plantTime = Time.time
        };
        
        crops[position] = crop;
        
        // Визуал растения
        GameObject plant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plant.transform.position = new Vector3(position.x, 0.3f, position.y);
        plant.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
        plant.GetComponent<Renderer>().material.color = Color.green;
        plant.transform.parent = cells[position].transform;
    }
    
    public int HarvestAtPosition(Vector2Int position)
    {
        if (!crops.ContainsKey(position)) return 0;
        
        CropData crop = crops[position];
        crops.Remove(position);
        
        // Удаляем визуал
        foreach (Transform child in cells[position].transform)
        {
            Destroy(child.gameObject);
        }
        
        // Ценность урожая
        return crop.type switch
        {
            "wheat" => 10,
            "corn" => 15,
            "carrot" => 20,
            _ => 5
        };
    }
    
    public bool HasCrop(Vector2Int position) => crops.ContainsKey(position);
}