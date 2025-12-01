using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class SC_GameLogic : MonoBehaviour
{
    public static SC_GameLogic Instance { get; private set; }
    
    [SerializeField] private Spawner<Transform> bgTileSpawner;
    [SerializeField] private Spawner<SC_Gem> gemSpawner;
    [SerializeField] private Spawner<Transform> destroyEffectSpawner;
    
    private Dictionary<string, GameObject> unityObjects;
    private int score = 0;
    private float displayScore = 0;
    private GameBoard gameBoard;
    private GlobalEnums.GameState currentState = GlobalEnums.GameState.move;
    
    public GlobalEnums.GameState CurrentState => currentState;
    public Spawner<Transform> DestroyEffectSpawner => destroyEffectSpawner;

    #region MonoBehaviour
    private void Awake()
    {
        Instance = this;
        Init();
    }

    private IEnumerator Start()
    {
        yield return null;
        StartGame();
    }

    private void Update()
    {
        displayScore = Mathf.Lerp(displayScore, gameBoard.Score, SC_GameVariables.Instance.scoreSpeed * Time.deltaTime);
        unityObjects["Txt_Score"].GetComponent<TMPro.TextMeshProUGUI>().text = displayScore.ToString("0");
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
                bgTileSpawner.Spawn(SC_GameVariables.Instance.bgTilePrefabs.transform, onInitialize: _bgTile =>
                {
                    _bgTile.transform.SetParent(unityObjects["GemsHolder"].transform);
                    _bgTile.transform.localPosition = new Vector2(x, y);;
                    _bgTile.transform.localRotation = Quaternion.identity;
                    _bgTile.name = "BG Tile - " + x + ", " + y;

                    int _gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);

                    int iterations = 0;
                    while (gameBoard.MatchesAt(new Vector2Int(x, y), SC_GameVariables.Instance.gems[_gemToUse]) && iterations < 100)
                    {
                        _gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);
                        iterations++;
                    }
                    SpawnGem(new Vector2Int(x, y), y + SC_GameVariables.Instance.dropHeight, SC_GameVariables.Instance.gems[_gemToUse]);
                });
                yield return new WaitForSeconds(.01f);
            }
    }
    
    public void StartGame()
    {
        unityObjects["Txt_Score"].GetComponent<TextMeshProUGUI>().text = score.ToString("0");
    }
    
    private void SpawnGem(Vector2Int _Position, float spawnYPos, SC_Gem _GemToSpawn)
    {
        if (Random.Range(0, 100f) < SC_GameVariables.Instance.bombChance)
            _GemToSpawn = SC_GameVariables.Instance.GetBomb(GlobalEnums.GemType.normalBomb);

        gemSpawner.Spawn(_GemToSpawn, onInitialize: _gem =>
        {
            _gem.transform.SetParent(unityObjects["GemsHolder"].transform);
            _gem.transform.position = new Vector3(_Position.x, spawnYPos, 0f);
            _gem.transform.localRotation = Quaternion.identity;
            _gem.name = "Gem - " + _Position.x + ", " + _Position.y;
            gameBoard.SetGem(_Position.x,_Position.y, _gem);
            _gem.SetupGem(this,_Position);
        });
    }
    public void SetGem(int _X,int _Y, SC_Gem _Gem)
    {
        gameBoard.SetGem(_X,_Y, _Gem);
    }
    public SC_Gem GetGem(int _X, int _Y)
    {
        return gameBoard.GetGem(_X, _Y);
    }
    public void SetState(GlobalEnums.GameState _CurrentState)
    {
        currentState = _CurrentState;
    }
    
    public void StartDestroyMatches(SC_Gem triggeredGem = null, SC_Gem otherGem = null)
        => StartCoroutine(_StartDestroyMatches(triggeredGem, otherGem));

    private IEnumerator _StartDestroyMatches(SC_Gem triggeredGem = null, SC_Gem otherGem = null)
    {
        // Destroy all matches
        DestroyMatches(false, triggeredGem, otherGem);
        
        // Check for bombs and destroy them
        while (gameBoard.CheckForBombs())
        {
            yield return new WaitForSeconds(0.5f);
            DestroyMatches(true, null, null);
            gameBoard.NewlyCreatedBombs.Clear();
        }

        // Safely clears the newly created bombs cache
        gameBoard.NewlyCreatedBombs.Clear();
        
        yield return new WaitForSeconds(.2f);
        
        // Decrease row positions and fill empty spaces
        yield return DecreaseRowCo();
        yield return FilledBoardCo();
    }
    
    private void DestroyMatches(bool destroyBombs, SC_Gem triggeredGem, SC_Gem otherGem)
    {
        for (int i = 0; i < gameBoard.CurrentMatches.Count; i++)
        {
            if (gameBoard.CurrentMatches[i] != null && (destroyBombs || !gameBoard.CurrentMatches[i].isBomb))
            {
                ScoreCheck(gameBoard.CurrentMatches[i]);
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
                        gemSpawner.Spawn(SC_GameVariables.Instance.GetBomb(userActionGem.type), onInitialize: _gem =>
                        {
                            var _Position = userActionGem.posIndex;
                            _gem.transform.SetParent(unityObjects["GemsHolder"].transform);
                            _gem.transform.position = new Vector3(_Position.x, _Position.y, 0f);
                            _gem.transform.localRotation = Quaternion.identity;
                            _gem.name = "Gem - " + _Position.x + ", " + _Position.y;
                            gameBoard.SetGem(_Position.x,_Position.y, _gem);
                            _gem.SetupGem(this,_Position);
                            
                            // Add to newly create bombs cache
                            gameBoard.NewlyCreatedBombs.Add(_gem);
                        });
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
                    yield return new WaitForSeconds(.05f);
                }
            }
            nullCounter = 0;
        }
    }

    public void ScoreCheck(SC_Gem gemToCheck)
    {
        gameBoard.Score += gemToCheck.scoreValue;
    }
    private void DestroyMatchedGemsAt(Vector2Int _Pos)
    {
        SC_Gem _curGem = gameBoard.GetGem(_Pos.x,_Pos.y);
        if (_curGem != null)
        {
            destroyEffectSpawner.Spawn(_curGem.destroyEffect.transform, 
                onInitialize: _effect =>
                {
                    _effect.localPosition = new Vector2(_Pos.x, _Pos.y);
                    _effect.localRotation = Quaternion.identity;
                });
            gemSpawner.Despawn(_curGem);
            SetGem(_Pos.x,_Pos.y, null);
        }
    }

    private IEnumerator FilledBoardCo()
    {
        yield return new WaitForSeconds(0.05f);
        yield return RefillBoard();
        yield return new WaitForSeconds(0.5f);
        gameBoard.FindAllMatches();
        if (gameBoard.CurrentMatches.Count > 0)
            StartDestroyMatches();
        else
            currentState = GlobalEnums.GameState.move;
    }
    private IEnumerator RefillBoard()
    {
        List<Vector2Int> requestPositions = new List<Vector2Int>();
        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem _curGem = gameBoard.GetGem(x,y);
                if (_curGem == null)
                {
                    requestPositions.Add(new Vector2Int(x,y));
                }
            }
        }
        
        requestPositions = requestPositions
            .OrderBy(p => p.y)   // bottom to top
            .ThenBy(p => p.x)    // left to right
            .ToList();
        
        var rowGroup = new List<List<Vector2Int>>();
        for (var col = 0; col < SC_GameVariables.Instance.colsSize; col++)
            rowGroup.Add(requestPositions.Where(x => x.x == col).ToList());
        
        foreach (var row in rowGroup)
        {
            for (var index = 0; index < row.Count; index++)
            {
                var cell = row[index];
                var bottomAndSideGems = GetGemsBottomAndSide(cell);
                var gemsToSpawn = SC_GameVariables.Instance.gems
                    .Where(x => !bottomAndSideGems.Contains(x.type))
                    .ToList();

                int gemToUse = Random.Range(0, gemsToSpawn.Count);
                SpawnGem(cell, index + SC_GameVariables.Instance.dropHeight,  gemsToSpawn[gemToUse]);
                yield return new WaitForSeconds(.05f);
            }
        }

        CheckMisplacedGems();
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
            gemSpawner.Despawn(g);
    }
    public void FindAllMatches()
    {
        gameBoard.FindAllMatches();
    }

    #endregion
}
