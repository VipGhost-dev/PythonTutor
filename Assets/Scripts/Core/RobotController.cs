// Assets/Scripts/Core/RobotController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RobotController : MonoBehaviour
{
    private const float ROBOT_HEIGHT = 0.25f;
    private Vector2Int facingDirection = Vector2Int.up;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 180f;
    public float accelerationTime = 0.3f;
    
    [Header("Wheels")]
    public GameObject[] wheels;
    public float wheelSpinSpeed = 360f;
    
    [Header("Camera")]
    public GameObject cameraObject;
    
    [Header("Components")]
    public FarmGrid farmGrid;
    
    private Vector2Int gridPosition;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isMoving = false;
    private Queue<Vector2Int> moveQueue = new Queue<Vector2Int>();
    private float currentSpeed = 0f;
    
    // События
    public System.Action<Vector2Int> OnPositionChanged;
    public System.Action OnMovementComplete;
    
    void Start()
    {
        if (farmGrid == null)
            farmGrid = FindObjectOfType<FarmGrid>();
        
        // Стартовая позиция
        if (farmGrid != null)
        {
            gridPosition = farmGrid.GetRandomSoilPosition();
            transform.position = new Vector3(gridPosition.x, ROBOT_HEIGHT, gridPosition.y);
            targetPosition = new Vector3(gridPosition.x, ROBOT_HEIGHT, gridPosition.y);
        }
    }
    
    void Update()
    {
        Debug.Log($"Update: isMoving={isMoving}, queue={moveQueue.Count}");

        if (moveQueue.Count > 0)
        {
            Debug.Log($"QUEUE={moveQueue.Count}, MOVING={isMoving}");
        }

        if (!isMoving && moveQueue.Count > 0)
        {
            StartCoroutine(MoveToPosition(moveQueue.Dequeue()));
        }
        
        if (isMoving)
        {
            // Плавное движение с ускорением
            currentSpeed = Mathf.Lerp(currentSpeed, moveSpeed, Time.deltaTime / accelerationTime);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            float dist = Vector3.Distance(transform.position, targetPosition);

            if (Time.frameCount % 30 == 0)
            {
                Debug.Log(
                    $"POS={transform.position} TARGET={targetPosition} DIST={dist}"
                );
            }

            // Вращаем колёса
            RotateWheels();
            
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                Debug.Log("TARGET REACHED");
                
                transform.position = targetPosition;
                isMoving = false;
                currentSpeed = 0f;

                OnMovementComplete?.Invoke();
            }
        }
    }
    public void TurnLeft()
    {
        facingDirection = new Vector2Int(
            -facingDirection.y,
            facingDirection.x
        );

        UpdateRotation();
    }

    public void TurnRight()
    {
        facingDirection = new Vector2Int(
            facingDirection.y,
            -facingDirection.x
        );

        UpdateRotation();
    }

    private void UpdateRotation()
    {
        Vector3 lookDir = new Vector3(
            facingDirection.x,
            0,
            facingDirection.y
        );

        transform.rotation =
            Quaternion.LookRotation(lookDir);
    }

    public bool MoveForward()
{
    return Move(facingDirection);
}
    
    IEnumerator MoveToPosition(Vector2Int newGridPos)
    {
        Debug.Log($"MoveToPosition START {newGridPos}");

        if (isMoving) yield break;
        
        // Проверка на возможность движения
        if (farmGrid != null && !farmGrid.IsWalkable(newGridPos))
        {
            Debug.Log($"Cannot move to {newGridPos} - blocked");
            yield break;
        }
        
        isMoving = true;
        
        // Обновляем позицию
        gridPosition = newGridPos;
        
        // Вычисляем целевую позицию
        targetPosition = new Vector3(newGridPos.x, ROBOT_HEIGHT, newGridPos.y);
        
        // Поворачиваемся к цели
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(direction);
        }
        
        // Анимация наклона камеры
        if (cameraObject != null)
        {
            StartCoroutine(TiltCamera(direction));
        }
        
        // Ждём завершения движения
        yield return new WaitWhile(() => isMoving);
        
        // Событие об изменении позиции
        OnPositionChanged?.Invoke(gridPosition);
        
        // Небольшая задержка перед следующим движением
        yield return new WaitForSeconds(0.1f);

        Debug.Log($"Reached {gridPosition}");   
    }
    
    void RotateWheels()
    {
        float spin = wheelSpinSpeed * Time.deltaTime;
        foreach (GameObject wheel in wheels)
        {
            if (wheel != null)
                wheel.transform.Rotate(Vector3.right, spin);
        }
    }
    
    IEnumerator TiltCamera(Vector3 direction)
    {
        if (cameraObject == null) yield break;
        
        Quaternion originalRotation = cameraObject.transform.localRotation;
        Quaternion targetTilt = Quaternion.Euler(direction.z * 15f, direction.x * 10f, 0);
        
        float elapsed = 0;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cameraObject.transform.localRotation = Quaternion.Slerp(originalRotation, targetTilt, t);
            yield return null;
        }
        
        elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cameraObject.transform.localRotation = Quaternion.Slerp(targetTilt, originalRotation, t);
            yield return null;
        }
        
        cameraObject.transform.localRotation = originalRotation;
    }
    
    public bool Move(Vector2Int direction)
    {
        Debug.Log($"Move request. Current={gridPosition} Direction={direction}");

        Vector2Int newPos = gridPosition + direction;

        Debug.Log($"Trying move to {newPos}");
        
        if (farmGrid != null && farmGrid.IsWalkable(newPos))
        {
            moveQueue.Enqueue(newPos);
            Debug.Log($"Queued move to {newPos}");
            return true;
        }
        
        Debug.Log($"Cannot move to {newPos} - blocked");
        Debug.Log($"Move blocked: {newPos}");
        return false;
    }
    
    public Vector2Int GetPosition()
    {
        return gridPosition;
    }
    
    public bool IsMoving()
    {
        return isMoving;
    }
    
    public void Stop()
    {
        moveQueue.Clear();
    }
    
    public void Teleport(Vector2Int newPosition)
    {
        if (farmGrid != null && !farmGrid.IsWalkable(newPosition)) return;
        
        gridPosition = newPosition;
        transform.position = new Vector3(newPosition.x, ROBOT_HEIGHT, newPosition.y);
        moveQueue.Clear();
        isMoving = false;
        currentSpeed = 0f;
    }
}