using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RobotController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stepDelay = 0.2f;
    
    [Header("Visuals")]
    public GameObject body;
    public ParticleSystem moveParticles;
    
    private Vector2Int gridPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool canMove = true;
    private Queue<Vector2Int> moveQueue = new Queue<Vector2Int>();
    private FarmGrid farmGrid;
    
    void Start()
    {
        farmGrid = FindObjectOfType<FarmGrid>();
        
        // Стартовая позиция - центр поля
        if (farmGrid != null)
        {
            gridPosition = new Vector2Int(farmGrid.width / 2, farmGrid.height / 2);
            transform.position = new Vector3(gridPosition.x, 0, gridPosition.y);
            targetPosition = transform.position;
        }
    }
    
    void Update()
    {
        if (!isMoving && moveQueue.Count > 0 && canMove)
        {
            StartCoroutine(MoveToPosition(moveQueue.Dequeue()));
        }
        
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
                canMove = true;
                
                if (moveParticles != null)
                    moveParticles.Stop();
            }
        }
    }
    
    IEnumerator MoveToPosition(Vector2Int newGridPos)
    {
        if (isMoving) yield break;
        
        isMoving = true;
        canMove = false;
        targetPosition = new Vector3(newGridPos.x, 0, newGridPos.y);
        
        if (moveParticles != null)
            moveParticles.Play();
        
        // Анимация поворота
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float elapsed = 0;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, elapsed / 0.1f);
                yield return null;
            }
        }
        
        yield return new WaitUntil(() => !isMoving);
        
        gridPosition = newGridPos;
        yield return new WaitForSeconds(stepDelay);
        canMove = true;
    }
    
    public bool Move(Vector2Int direction)
    {
        if (farmGrid == null) return false;
        
        Vector2Int newPos = gridPosition + direction;
        
        if (farmGrid.IsWalkable(newPos))
        {
            moveQueue.Enqueue(newPos);
            return true;
        }
        
        Debug.Log($"Cannot move to {newPos} - blocked");
        return false;
    }
    
    public Vector2Int GetPosition() => gridPosition;
    public bool IsMoving() => isMoving;
}