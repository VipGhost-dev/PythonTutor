using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RobotController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stepDelay = 0.2f;
    
    private Vector2Int gridPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool canMove = true;
    private Queue<Vector2Int> moveQueue = new Queue<Vector2Int>();
    
    void Start()
    {
        gridPosition = new Vector2Int(7, 7);
        transform.position = new Vector3(gridPosition.x, 0, gridPosition.y);
        targetPosition = transform.position;
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
            }
        }
    }
    
    IEnumerator MoveToPosition(Vector2Int newGridPos)
    {
        if (isMoving) yield break;
        
        isMoving = true;    
        canMove = false;
        targetPosition = new Vector3(newGridPos.x, 0, newGridPos.y);
        
        yield return new WaitUntil(() => !isMoving);
        
        gridPosition = newGridPos;
        yield return new WaitForSeconds(stepDelay);
        canMove = true;
    }
    
    public bool Move(Vector2Int direction)
    {
        Vector2Int newPos = gridPosition + direction;
        FarmGrid farmGrid = FindObjectOfType<FarmGrid>();
        
        if (farmGrid != null && farmGrid.IsWalkable(newPos))
        {
            moveQueue.Enqueue(newPos);
            return true;
        }
        
        return false;
    }
    
    public Vector2Int GetPosition() => gridPosition;
    public bool IsMoving() => isMoving;
}