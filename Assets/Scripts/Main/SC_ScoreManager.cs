using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SC_ScoreManager : MonoBehaviour
{
    public static SC_ScoreManager Instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI scoreLabel;
    
    private int score = 0;
    private float displayScore = 0;

    private void Awake() => Instance = this;

    private void Update()
    {
        displayScore = Mathf.Lerp(displayScore, score, SC_GameVariables.Instance.scoreSpeed * Time.deltaTime);
        scoreLabel.text = displayScore.ToString("0");
    }
    
    public void ResetValues()
    {
        score = 0;
        displayScore = 0;
        scoreLabel.text = score.ToString("0");
    }

    public void AddGemScore(SC_Gem gem) => score += gem.scoreValue;
    
}
