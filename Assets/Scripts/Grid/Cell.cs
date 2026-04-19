using UnityEngine;

public class Cell : MonoBehaviour
{
    [Header("Cell Settings")]
    public Vector2Int gridPosition;
    public CellType type;
    public bool isWalkable = true;
    public GameObject cropObject;
    public bool isFertile = false;  // Плодородная зона
    
    public enum CellType
    {
        Soil,      // Можно сажать, проходимо
        Grass,     // Нельзя ходить/сажать
        Stone      // Препятствие
    }
    
    void Start()
    {
        UpdateVisual();
    }
    
    public void SetType(CellType newType)
    {
        type = newType;
        isWalkable = (type == CellType.Soil);
        UpdateVisual();
    }
    
    void UpdateVisual()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;
        
        switch (type)
        {
            case CellType.Soil:
                if (isFertile)
                    renderer.material.color = new Color(0.7f, 0.5f, 0.2f); // Более тёмная плодородная земля
                else
                    renderer.material.color = new Color(0.6f, 0.4f, 0.2f); // Обычная земля
                break;
            case CellType.Grass:
                renderer.material.color = new Color(0.2f, 0.7f, 0.2f); // Ярко-зелёный
                isWalkable = false;
                break;
            case CellType.Stone:
                renderer.material.color = new Color(0.4f, 0.4f, 0.4f); // Серый
                isWalkable = false;
                break;
        }
    }
    
    public void SetCrop(GameObject crop)
    {
        cropObject = crop;
        if (crop != null)
        {
            crop.transform.SetParent(transform);
            crop.transform.localPosition = new Vector3(0, 0.3f, 0);
        }
    }
}