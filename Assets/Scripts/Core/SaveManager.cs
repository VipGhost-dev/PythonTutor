using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const string QuestKey = "CurrentQuest";

    public static void SaveQuest(int questIndex)
    {
        PlayerPrefs.SetInt(QuestKey, questIndex);
        PlayerPrefs.Save();
    }

    public static int LoadQuest()
    {
        return PlayerPrefs.GetInt(QuestKey, 0);
    }

    public static void ResetSave()
    {
        PlayerPrefs.DeleteKey(QuestKey);
        PlayerPrefs.Save();
    }
}
