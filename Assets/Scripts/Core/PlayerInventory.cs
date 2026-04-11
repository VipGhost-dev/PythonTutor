using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private int coins = 100;
    private Dictionary<string, int> seeds = new Dictionary<string, int>();
    
    void Start()
    {
        seeds["wheat"] = 5;
        seeds["corn"] = 3;
        seeds["carrot"] = 2;
    }
    
    public void AddCoins(int amount)
    {
        coins += amount;
        Debug.Log($"Coins: {coins}");
    }
    
    public bool SpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;
            return true;
        }
        return false;
    }
    
    public int GetCoins() => coins;
    
    public bool HasSeed(string seedType)
    {
        return seeds.ContainsKey(seedType) && seeds[seedType] > 0;
    }
    
    public void RemoveSeed(string seedType)
    {
        if (seeds.ContainsKey(seedType) && seeds[seedType] > 0)
        {
            seeds[seedType]--;
        }
    }
    
    public void AddSeed(string seedType, int amount)
    {
        if (!seeds.ContainsKey(seedType))
            seeds[seedType] = 0;
        seeds[seedType] += amount;
    }
}