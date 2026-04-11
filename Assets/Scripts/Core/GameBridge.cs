using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System;

public class GameBridge : MonoBehaviour
{
    [Header("References")]
    public RobotController robot;
    public FarmGrid farmGrid;
    public PlayerInventory inventory;
    public PythonBridge pythonBridge;
    public UIManager uiManager;
    
    private bool isExecuting = false;
    
    async void Start()
    {
        if (pythonBridge == null) pythonBridge = FindObjectOfType<PythonBridge>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        
        string testResult = await pythonBridge.TestConnection();
        Debug.Log($"Connection test: {testResult}");
        
        // Тестовый код через 1 секунду
        Invoke("RunPythonTest", 1f);
    }
    
    void RunPythonTest()
    {
        string testCode = @"
print('Hello from player code!')
print('Testing multiple lines')
for i in range(3):
    print(f'Number {i}')
print('Done!')
".TrimStart();
        
        ExecutePlayerCode(testCode);
    }
    
    public async void ExecutePlayerCode(string code)
    {
        if (isExecuting)
        {
            uiManager?.ShowMessage("Code is already executing!");
            return;
        }
        
        isExecuting = true;
        uiManager?.ShowMessage("Executing Python code...");
        
        // Очищаем код от лишних отступов
        string cleanedCode = CleanCodeIndentation(code);
        Debug.Log($"Cleaned code:\n{cleanedCode}");
        
        string result = await pythonBridge.ExecutePythonCode(cleanedCode);
        Debug.Log($"Raw response: {result}");
        
        // Простой парсинг ответа
        if (!string.IsNullOrEmpty(result))
        {
            if (result.Contains("\"success\": true") || result.Contains("\"success\":true"))
            {
                // Извлекаем output
                string output = ExtractJsonValue(result, "output");
                uiManager?.ShowMessage($"✅ Complete!\n{output}");
                uiManager?.AddConsoleOutput(output);
            }
            else
            {
                string error = ExtractJsonValue(result, "error");
                if (string.IsNullOrEmpty(error)) error = "Unknown error";
                uiManager?.ShowMessage($"❌ Error: {error}");
                Debug.LogError($"Python error: {error}");
            }
        }
        else
        {
            uiManager?.ShowMessage("❌ Error: Empty response from server");
        }
        
        isExecuting = false;
    }
    
    private string ExtractJsonValue(string json, string key)
    {
        try
        {
            string searchFor = $"\"{key}\":";
            int startIndex = json.IndexOf(searchFor);
            if (startIndex == -1) return "";
            
            startIndex += searchFor.Length;
            
            // Пропускаем пробелы
            while (startIndex < json.Length && (json[startIndex] == ' ' || json[startIndex] == '\t'))
                startIndex++;
            
            // Проверяем тип значения
            if (json[startIndex] == '"')
            {
                // Строковое значение
                startIndex++;
                int endIndex = json.IndexOf('"', startIndex);
                if (endIndex == -1) return "";
                return json.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                // Булево или числовое значение
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
    
    private string CleanCodeIndentation(string code)
    {
        if (string.IsNullOrEmpty(code))
            return code;
        
        string[] lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        
        // Находим минимальный отступ
        int minIndent = int.MaxValue;
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            int indent = 0;
            foreach (char c in line)
            {
                if (c == ' ' || c == '\t')
                    indent++;
                else
                    break;
            }
            if (indent < minIndent)
                minIndent = indent;
        }
        
        if (minIndent == int.MaxValue || minIndent == 0)
            return code;
        
        // Удаляем отступы
        var cleanedLines = new List<string>();
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                cleanedLines.Add("");
            }
            else
            {
                string cleanedLine = line.Length > minIndent ? line.Substring(minIndent) : line;
                cleanedLines.Add(cleanedLine);
            }
        }
        
        return string.Join("\n", cleanedLines);
    }
    
    // Методы для вызова из Python
    public string MoveRobot(string direction)
    {
        if (robot == null) return "ERROR: No robot";
        
        Vector2Int moveVector = direction.ToLower() switch
        {
            "up" => new Vector2Int(0, 1),
            "down" => new Vector2Int(0, -1),
            "left" => new Vector2Int(-1, 0),
            "right" => new Vector2Int(1, 0),
            _ => new Vector2Int(0, 0)
        };
        
        bool success = robot.Move(moveVector);
        return success ? "OK" : "BLOCKED";
    }
    
    public int HarvestCurrent()
    {
        if (farmGrid == null) return 0;
        
        Vector2Int pos = robot.GetPosition();
        int value = farmGrid.HarvestAtPosition(pos);
        
        if (value > 0)
        {
            inventory?.AddCoins(value);
            uiManager?.ShowFloatingText($"+{value}", robot.transform.position);
        }
        
        return value;
    }
    
    public bool PlantSeed(string seedType)
    {
        if (farmGrid == null || inventory == null) return false;
        
        Vector2Int pos = robot.GetPosition();
        
        if (!farmGrid.CanPlantAtPosition(pos))
        {
            uiManager?.ShowMessage("Cannot plant here!");
            return false;
        }
        
        if (!inventory.HasSeed(seedType))
        {
            uiManager?.ShowMessage($"No {seedType} seeds!");
            return false;
        }
        
        farmGrid.PlantAtPosition(pos, seedType);
        inventory.RemoveSeed(seedType);
        uiManager?.ShowMessage($"Planted {seedType}!");
        
        return true;
    }
    
    public string GetRobotPosition()
    {
        Vector2Int pos = robot.GetPosition();
        return $"{pos.x},{pos.y}";
    }
}