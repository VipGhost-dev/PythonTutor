using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField codeInput;
    public Button runButton;
    public Button resetButton;
    public TextMeshProUGUI outputText;
    public TextMeshProUGUI coinsText;
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    
    private GameBridge gameBridge;
    private PlayerInventory inventory;
    
    [Header("Settings UI")]

    public bool clearOutputOnRun = true;
    void Start()
    {
        gameBridge = FindObjectOfType<GameBridge>();
        inventory = FindObjectOfType<PlayerInventory>();
        
        if (runButton != null)
            runButton.onClick.AddListener(RunCode);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetGame);
        
        LoadExampleCode();
        
        // Скрываем панель сообщений
        if (messagePanel != null)
            messagePanel.SetActive(false);
        
        // Обновляем UI каждую секунду
        InvokeRepeating("UpdateUI", 0, 1f);
    }
    
    void UpdateUI()
    {
        if (inventory != null && coinsText != null)
        {
            coinsText.text = $"💰 {inventory.GetCoins()}";
        }
    }
    
    void RunCode()
    {
        if (gameBridge == null)
        {
            Debug.LogError("GameBridge not found!");
            return;
        }
        
        if (codeInput == null)
        {
            Debug.LogError("CodeInput not assigned!");
            return;
        }

        if (clearOutputOnRun)
        {
            ClearOutput();
        }
        
        string code = codeInput.text;
        gameBridge.ExecutePlayerCode(code);
    }

    public void ClearOutput()
    {
        if(outputText != null)
        {
            outputText.text = "";
        }
    }
    
    void ResetGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    
    void LoadExampleCode()
    {
        if (codeInput == null) return;
        
        codeInput.text = "# RoboFarmer Example\n" +
                         "# Move in a square pattern\n" +
                         "\n" +
                         "for i in range(4):\n" +
                         "    for j in range(3):\n" +
                         "        api.move('right')\n" +
                         "    \n" +
                         "    api.move('down')\n" +
                         "    \n" +
                         "    for j in range(3):\n" +
                         "        api.move('left')\n" +
                         "    \n" +
                         "    api.move('down')\n" +
                         "\n" +
                         "print('Harvesting complete!')";
    }
    
    public void ShowMessage(string message)
    {
        Debug.Log($"Message: {message}");
        
        if (messageText != null)
            messageText.text = message;
        
        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
            StartCoroutine(HideMessageAfterDelay(2f));
        }
    }
    
    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }
    
    public void ShowFloatingText(string text, Vector3 worldPosition)
    {
        // Можно реализовать позже
        Debug.Log($"Floating text: {text}");
    }
    
    public void AddConsoleOutput(string output)
    {
        if (string.IsNullOrEmpty(output)) return;
        
        Debug.Log($"Console output:\n{output}");
        
        if (outputText != null)
        {
            // Добавляем новую строку сверху
            outputText.text = output + "\n\n" + outputText.text;
            
            // Ограничиваем количество строк
            string[] lines = outputText.text.Split('\n');
            if (lines.Length > 50)
            {
                outputText.text = string.Join("\n", lines, 0, 50);
            }
        }
        else
        {
            Debug.LogWarning("OutputText is not assigned in UIManager!");
        }
    }
}