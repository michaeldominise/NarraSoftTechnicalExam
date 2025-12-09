using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class SC_GameLogic : MonoBehaviour
{
    public static SC_GameLogic Instance { get; private set; }
    
    private Dictionary<string, GameObject> unityObjects;
    private GameBoard gameBoard;
    private GlobalEnums.GameState currentState = GlobalEnums.GameState.move;
    
    public GlobalEnums.GameState CurrentState => currentState;

    #region MonoBehaviour
    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return null; 
        Init();
        SC_ScoreManager.Instance.ResetValues();
    }
    #endregion

    #region Logic
    private void Init()
    {
        unityObjects = new Dictionary<string, GameObject>();
        GameObject[] _obj = GameObject.FindGameObjectsWithTag("UnityObject");
        foreach (GameObject g in _obj)
            unityObjects.Add(g.name,g);

        gameBoard = new GameBoard(7, 7);
        StartCoroutine(Setup());
    }
    
    private IEnumerator Setup()
    {
        for (int x = 0; x < gameBoard.Width; x++)
            for (int y = 0; y < gameBoard.Height; y++)
            {
                var position = new Vector2Int(x, y);
                SC_Spawner.Instance.SpawnBGTile(position, unityObjects["GemsHolder"].transform);
                
                
                SpawnGem(new Vector2Int(x, y), y + SC_GameVariables.Instance.dropHeight, GetNonMatchingGem(position));
                yield return new WaitForSeconds(SC_GameVariables.Instance.setupDropInterval);
            }
    }

    private SC_Gem GetNonMatchingGem(Vector2Int position)
    {
        int _gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);

        int iterations = 0;
        while (gameBoard.MatchesAt(position, SC_GameVariables.Instance.gems[_gemToUse]) && iterations < 100)
        {
            _gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);
            iterations++;
        }
        
        return SC_GameVariables.Instance.gems[_gemToUse];
    }
    
    private void SpawnGem(Vector2Int _Position, float spawnYPos, SC_Gem _GemToSpawn)
    {
        if (Random.Range(0, 100f) < SC_GameVariables.Instance.bombChance)
            _GemToSpawn = SC_GameVariables.Instance.GetBomb(GlobalEnums.GemType.normalBomb);

        var _gem = SC_Spawner.Instance.SpawnGem(_GemToSpawn, _Position, spawnYPos, unityObjects["GemsHolder"].transform);
        SetGem(_Position.x,_Position.y, _gem);
    }
    
    public void SetGem(int _X,int _Y, SC_Gem _Gem) => gameBoard.SetGem(_X,_Y, _Gem);
    public SC_Gem GetGem(int _X, int _Y) => gameBoard.GetGem(_X, _Y);
    public void SetState(GlobalEnums.GameState _CurrentState) => currentState = _CurrentState;
    
    public void StartDestroyMatches(SC_Gem triggeredGem = null, SC_Gem otherGem = null)
        => StartCoroutine(_StartDestroyMatches(triggeredGem, otherGem));

    private IEnumerator _StartDestroyMatches(SC_Gem triggeredGem = null, SC_Gem otherGem = null)
    {
        // Destroy all matches
        DestroyMatches(false, triggeredGem, otherGem);
        
        // Check for bombs and destroy them
        if (gameBoard.CheckForBombs())
        {
            yield return new WaitForSeconds(SC_GameVariables.Instance.bombCheckInterval);
            DestroyMatches(true, null, null);
        }

        // Safely clears the newly created bombs cache
        gameBoard.NewlyCreatedBombs.Clear();
        
        yield return new WaitForSeconds(SC_GameVariables.Instance.gemDropInterval);
        
        // Decrease row positions and fill empty spaces
        StartCoroutine(DecreaseRowCo());
        StartCoroutine(FilledBoardCo());
        
    }
    
    private void DestroyMatches(bool destroyBombs, SC_Gem triggeredGem, SC_Gem otherGem)
    {
        for (int i = 0; i < gameBoard.CurrentMatches.Count; i++)
        {
            if (gameBoard.CurrentMatches[i] != null && (destroyBombs || !gameBoard.CurrentMatches[i].isBomb))
            {
                SC_ScoreManager.Instance.AddGemScore(gameBoard.CurrentMatches[i]);
                DestroyMatchedGemsAt(gameBoard.CurrentMatches[i].posIndex);
            }
        }

        var userActionGems = new List<SC_Gem> { triggeredGem, otherGem };
        foreach (var userActionGem in userActionGems)
        {
            if (userActionGem != null)
                foreach (var groupMatch in gameBoard.CurrentGroupMatches)
                {
                    if (groupMatch.Contains(userActionGem) && groupMatch.Count >= 4)
                    {
                        gameBoard.CurrentMatches.Remove(userActionGem);
                        var _gem = SC_Spawner.Instance.SpawnGem(SC_GameVariables.Instance.GetBomb(userActionGem.type), userActionGem.posIndex, userActionGem.posIndex.y, unityObjects["GemsHolder"].transform);
                        gameBoard.SetGem(userActionGem.posIndex.x,userActionGem.posIndex.y, _gem);
                        gameBoard.NewlyCreatedBombs.Add(_gem);
                        break;
                    }
                }
        }
    }
    
    private IEnumerator DecreaseRowCo()
    {
        int nullCounter = 0;
        
        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem _curGem = gameBoard.GetGem(x, y);
                if (_curGem == null)
                {
                    nullCounter++;
                }
                else if (nullCounter > 0)
                {
                    _curGem.posIndex.y -= nullCounter;
                    SetGem(x, y - nullCounter, _curGem);
                    SetGem(x, y, null);
                    yield return new WaitForSeconds(SC_GameVariables.Instance.gemDropInterval);
                }
            }
            nullCounter = 0;
        
            var refillCounter = 0;
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem _curGem = gameBoard.GetGem(x, y);
                if (_curGem != null)
                    continue;
                var position = new Vector2Int(x, y);
                var bottomAndSideGems = GetGemsBottomAndSide(position);
                var gemsToSpawn = SC_GameVariables.Instance.gems
                    .Where(x => !bottomAndSideGems.Contains(x.type))
                    .ToList();

                int gemToUse = Random.Range(0, gemsToSpawn.Count);
                SpawnGem(position, refillCounter + SC_GameVariables.Instance.dropHeight,  gemsToSpawn[gemToUse]);
                yield return new WaitForSeconds(SC_GameVariables.Instance.gemDropInterval);
                refillCounter++;
            }
        }
    }
    
    private void DestroyMatchedGemsAt(Vector2Int _Pos)
    {
        SC_Gem _curGem = gameBoard.GetGem(_Pos.x,_Pos.y);
        if (_curGem != null)
        {
            SC_Spawner.Instance.SpawnDestroyParticle(_curGem.destroyEffect.transform, _Pos);
            SC_Spawner.Instance.DespawnGem(_curGem);
            SetGem(_Pos.x,_Pos.y, null);
        }
    }

    private IEnumerator FilledBoardCo()
    {
        yield return new WaitForSeconds(SC_GameVariables.Instance.findAllMatchesDelay);
        gameBoard.FindAllMatches();
        if (gameBoard.CurrentMatches.Count > 0)
            StartDestroyMatches();
        else
            currentState = GlobalEnums.GameState.move;
    }

    // returns a list of gems that are on the bottom and side of the given position
    public List<GlobalEnums.GemType> GetGemsBottomAndSide(Vector2Int _Pos)
    {
        List<GlobalEnums.GemType> gems = new List<GlobalEnums.GemType>();

        var right = gameBoard.GetGem(_Pos.x + 1, _Pos.y);
        var left = gameBoard.GetGem(_Pos.x - 1, _Pos.y);
        var bottom = gameBoard.GetGem(_Pos.x, _Pos.y - 1);
        
        if (right != null) gems.Add(right.type);
        if (left != null) gems.Add(left.type);
        if (bottom != null) gems.Add(bottom.type);
        
        return gems;
    }
    
    private void CheckMisplacedGems()
    {
        List<SC_Gem> foundGems = new List<SC_Gem>();
        foundGems.AddRange(FindObjectsOfType<SC_Gem>());
        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem _curGem = gameBoard.GetGem(x, y);
                if (foundGems.Contains(_curGem))
                    foundGems.Remove(_curGem);
            }
        }

        foreach (SC_Gem g in foundGems)
            SC_Spawner.Instance.DespawnGem(g);
    }
    public void FindAllMatches(SC_Gem gem1, SC_Gem gem2) => gameBoard.FindAllMatches(gem1, gem2);

    #endregion
}
