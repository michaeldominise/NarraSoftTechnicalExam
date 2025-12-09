using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SC_GameVariables : MonoBehaviour
{
    public GameObject bgTilePrefabs;
    public SC_Gem[] bombs;
    public SC_Gem[] gems;
    public float bonusAmount = 0.5f;
    public float bombChance = 2f;
    public int dropHeight = 0;
    public float gemSpeed;
    public float scoreSpeed = 5;
    
    [HideInInspector]
    public int rowsSize = 7;
    [HideInInspector]
    public int colsSize = 7;

    [Header("Gameplay Delay Intervals")] 
    public float setupDropInterval = 0.01f;
    public float bombCheckInterval = 0.5f;
    public float decreaseRowCoDelay = 0.2f;
    public float gemDropInterval = 0.05f;
    public float refillBoardDelay = 0.05f;
    public float findAllMatchesDelay = 0.5f;

    #region Singleton

    static SC_GameVariables instance;
    public static SC_GameVariables Instance
    {
        get
        {
            if (instance == null)
                instance = GameObject.Find("SC_GameVariables").GetComponent<SC_GameVariables>();

            return instance;
        }
    }

    #endregion

    public SC_Gem GetBomb(GlobalEnums.GemType _GemType)
        => bombs.FirstOrDefault(x => x.isBomb && x.type == _GemType);
}
