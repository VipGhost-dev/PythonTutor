using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System;
using System.Collections;

[System.Serializable]
public class CommandData
{
    public string action;
    public string direction;
    public string seed;
    public float seconds;
}

[System.Serializable]
public class PythonResponse
{
    public bool success;
    public string output;
    public CommandData[] commands;
}

public class GameBridge : MonoBehaviour
{
    [Header("References")]
    public RobotController robot;
    public FarmGrid farmGrid;
    public PythonBridge pythonBridge;
    public UIManager uiManager;
    public QuestManager questManager;

    private bool isExecuting = false;
    
    async void Start()
    {
        if (pythonBridge == null) pythonBridge = FindObjectOfType<PythonBridge>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (robot == null) robot = FindObjectOfType<RobotController>();
        if (farmGrid == null) farmGrid = FindObjectOfType<FarmGrid>();
        if (questManager == null) questManager = FindObjectOfType<QuestManager>();

        if (robot != null)
        {
            robot.OnPositionChanged += OnRobotPositionChanged;
            robot.OnMovementComplete += OnRobotMovementComplete;
        }
        
        string testResult = await pythonBridge.TestConnection();
        Debug.Log($"Connection test: {testResult}");
        
        Invoke("LoadDemoCode", 1f);
    }
    
    void OnRobotPositionChanged(Vector2Int newPosition)
    {
        Debug.Log($"Robot moved to: {newPosition}");
        uiManager?.AddConsoleOutput($"Position: {newPosition.x}, {newPosition.y}");
    }
    
    void OnRobotMovementComplete()
    {
        Debug.Log("Movement completed");
    }
    
    // ============ МЕТОДЫ ДЛЯ ВЫЗОВА ИЗ PYTHON ============
    public string MoveForward()
    {
        bool success = robot.MoveForward();

        return success ? "OK" : "BLOCKED";
    }

    public void TurnLeft()
    {
        robot.TurnLeft();
    }

    public void TurnRight()
    {
        robot.TurnRight();
    }

    public string MoveRobot(string direction)
    {
        if (robot == null) return "ERROR: Robot not found";
        
        Debug.Log($"🎮 MoveRobot called: {direction}");
        
        Vector2Int moveVector = direction.ToLower() switch
        {
            "up" => new Vector2Int(0, 1),
            "down" => new Vector2Int(0, -1),
            "left" => new Vector2Int(-1, 0),
            "right" => new Vector2Int(1, 0),
            _ => new Vector2Int(0, 0)
        };
        
        bool success = robot.Move(moveVector);
        
        if (success)
        {
            uiManager?.LogInfo($"Moving {direction}");
            return "OK";
        }
        else
        {
            uiManager?.LogWarning($"Cannot move {direction} - blocked!");
            return "BLOCKED";
        }
    }
    
    public bool PlantSeed(string seedType)
    {
        if (farmGrid == null || robot == null) return false;
        
        Vector2Int pos = robot.GetPosition();
        
        if (!farmGrid.CanPlantAtPosition(pos))
        {
            uiManager?.LogWarning("Cannot plant here!");
            return false;
        }
        
        farmGrid.PlantAtPosition(pos, seedType);
        uiManager?.LogSuccess($"Planted {seedType}!");
        
        return true;
    }
    
    public string GetRobotPosition()
    {
        if (robot == null) return "0,0";
        Vector2Int pos = robot.GetPosition();
        return $"{pos.x},{pos.y}";
    }
    
    public string ScanArea(int radius)
    {
        if (farmGrid == null || robot == null) return "{}";
        
        Vector2Int center = robot.GetPosition();
        var result = new Dictionary<string, object>();
        var cells = new List<object>();
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);
                var cellData = new Dictionary<string, object>
                {
                    ["x"] = pos.x,
                    ["y"] = pos.y,
                    ["has_crop"] = farmGrid.HasCrop(pos),
                    ["can_plant"] = farmGrid.CanPlantAtPosition(pos),
                    ["is_walkable"] = farmGrid.IsWalkable(pos),
                    ["is_soil"] = farmGrid.IsSoil(pos)
                };
                cells.Add(cellData);
            }
        }
        
        result["center"] = new { x = center.x, y = center.y };
        result["radius"] = radius;
        result["cells"] = cells;
        
        return JsonUtility.ToJson(result);
    }
    
    public void Wait(float seconds)
    {
        System.Threading.Thread.Sleep((int)(seconds * 1000));
    }
    
    // ============ ВЫПОЛНЕНИЕ КОДА ============
    
    public async void ExecutePlayerCode(string code)
    {
        if (isExecuting)
        {
            uiManager?.LogInfo("Code is already executing!");
            return;
        }
        
        isExecuting = true;
        uiManager?.LogInfo("Executing Python code...");
        
        string cleanedCode = CleanCodeIndentation(code);
        Debug.Log($"Executing code:\n{cleanedCode}");
        
        string result = await pythonBridge.ExecutePythonCode(cleanedCode);
        Debug.Log($"Raw response: {result}");

        Debug.Log(result);
        
        if (!string.IsNullOrEmpty(result))
        {
            if (result.Contains("\"success\": true") || result.Contains("\"success\":true"))
            {
                PythonResponse response =
                    JsonUtility.FromJson<PythonResponse>(result);

                if (response.commands != null &&
                    response.commands.Length > 0)
                {
                    StartCoroutine(
                        ExecuteCommands(response.commands)
                    );
                }

                Debug.Log($"Commands count: {response.commands?.Length}");
                string output = ExtractJsonValue(result, "output");
                uiManager?.LogSuccess($"Complete!");
                uiManager?.AddConsoleOutput(output);
            }
            else
            {
                string error = ExtractJsonValue(result, "error");
                if (string.IsNullOrEmpty(error)) error = "Unknown error";
                uiManager?.LogError($"Error: {error}");
                Debug.LogError($"Python error: {error}");
            }
        }
        else
        {
            uiManager?.LogError("Error: Empty response from server");
        }
        
        isExecuting = false;
    }
    
    private string CleanCodeIndentation(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;
        
        string[] lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        
        int minIndent = int.MaxValue;
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            int indent = 0;
            foreach (char c in line)
            {
                if (c == ' ' || c == '\t') indent++;
                else break;
            }
            if (indent < minIndent) minIndent = indent;
        }
        
        if (minIndent == int.MaxValue || minIndent == 0) return code;
        
        var cleanedLines = new List<string>();
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                cleanedLines.Add("");
            else
                cleanedLines.Add(line.Length > minIndent ? line.Substring(minIndent) : line);
        }
        
        return string.Join("\n", cleanedLines);
    }
    
    private string ExtractJsonValue(string json, string key)
    {
        try
        {
            string searchFor = $"\"{key}\":";
            int startIndex = json.IndexOf(searchFor);
            if (startIndex == -1) return "";
            
            startIndex += searchFor.Length;
            while (startIndex < json.Length && (json[startIndex] == ' ' || json[startIndex] == '\t'))
                startIndex++;
            
            if (json[startIndex] == '"')
            {
                startIndex++;
                int endIndex = json.IndexOf('"', startIndex);
                if (endIndex == -1) return "";
                return json.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                int endIndex = startIndex;
                while (endIndex < json.Length && json[endIndex] != ',' && json[endIndex] != '}')
                    endIndex++;
                return json.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Extract error: {e.Message}");
            return "";
        }
    }
    
    void OnDestroy()
    {
        if (robot != null)
        {
            robot.OnPositionChanged -= OnRobotPositionChanged;
            robot.OnMovementComplete -= OnRobotMovementComplete;
        }
    }

    private IEnumerator ExecuteCommands(CommandData[] commands)
    {
        foreach (var cmd in commands)
        {
            Debug.Log($"Executing: {cmd.action}");

            switch (cmd.action)
            {
                case "move":

                    MoveRobot(cmd.direction);

                    yield return new WaitUntil(
                        () => !robot.IsMoving()
                    );

                    Debug.Log("MOVE FINISHED");

                    break;

                case "plant":

                    PlantSeed(cmd.seed);

                    questManager?.CheckQuest();

                    yield return new WaitForSeconds(0.2f);

                    break;

                case "wait":

                    yield return new WaitForSeconds(cmd.seconds);

                    break;
                case "forward":

                    robot.MoveForward();

                    yield return new WaitUntil(
                        () => !robot.IsMoving()
                    );

                    break;

                case "turn_left":

                    robot.TurnLeft();

                    questManager?.CheckQuest();

                    yield return new WaitForSeconds(0.2f);

                    break;

                case "turn_right":

                    robot.TurnRight();

                    questManager?.CheckQuest();

                    yield return new WaitForSeconds(0.2f);             

                    break;
            }
        }
    }
}