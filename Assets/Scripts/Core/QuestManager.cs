using UnityEngine;
using TMPro;
using System.Collections;

public class QuestManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI questHintText;

    [Header("References")]
    public RobotController robot;
    public FarmGrid farmGrid;

    public int currentQuest = 0;
    private Vector2Int startPosition;
    private Vector2Int startFacingDirection;

    IEnumerator Start()
    {
        yield return null;

        if (robot == null) robot = FindObjectOfType<RobotController>();
        if (farmGrid == null) farmGrid = FindObjectOfType<FarmGrid>();

        currentQuest = SaveManager.LoadQuest();

        startPosition = robot.GetPosition();
        startFacingDirection = robot.GetFacingDirection();

        robot.OnPositionChanged += OnRobotPositionChanged;

        ShowQuest();
    }

    private void OnRobotPositionChanged(Vector2Int newPosition)
    {
        CheckQuest();
    }

    private void OnDestroy()
    {
        if (robot != null) robot.OnPositionChanged -= OnRobotPositionChanged;
    }

    void ShowQuest()
    {
        switch (currentQuest)
        {
            case 0:
                questTitleText.text = "Крутимся-вертимся";
                questDescriptionText.text = "Поверни направо";
                questHintText.text = "Подсказка:\napi.turn_right()";
                break;

            case 1:
                questTitleText.text = "Поехали!";
                questDescriptionText.text = "Проедь на 2 клетки вперёд.";
                questHintText.text = "Подсказка:\napi.forward()\napi.forward()";
                break;

            case 2:
                questTitleText.text = "Пора сажать";
                questDescriptionText.text = "Посади пшеницу.";
                questHintText.text = "Подсказка:\napi.plant(\"wheat\")";
                break;

            default:
                questTitleText.text = "Все задания выполнены!";
                questDescriptionText.text = "Отличная работа.";
                questHintText.text = "";
                break;
        }
    }

    public void CheckQuest()
    {
        Vector2Int pos = robot.GetPosition();

        bool completed = false;

        switch (currentQuest)
        {
            case 0:
                Vector2Int currentFacing = robot.GetFacingDirection();

                Vector2Int expectedFacing = new Vector2Int(
                    startFacingDirection.y,
                    -startFacingDirection.x
                );

                Debug.Log($"Quest 1 check. StartFacing={startFacingDirection}, Expected={expectedFacing}, Current={currentFacing}");

                completed = currentFacing == expectedFacing;
                break;
                
            case 1:
                Vector2Int targetPosition = startPosition + startFacingDirection * 2;

                Debug.Log($"Quest 2 check. Start={startPosition}, Facing={startFacingDirection}, Target={targetPosition}, Current={pos}");

                completed = pos == targetPosition;
                break;

            case 2:
                completed = farmGrid != null &&
                farmGrid.HasCropOfType(robot.GetPosition(), "wheat");

                Debug.Log($"Quest 3 check. Position={robot.GetPosition()}, WheatPlanted={completed}");
                break;
        }

        if (completed)
        {
            currentQuest++;

            SaveManager.SaveQuest(currentQuest);

            startPosition = robot.GetPosition();
            startFacingDirection = robot.GetFacingDirection();

            ShowQuest();
        }
    }
}