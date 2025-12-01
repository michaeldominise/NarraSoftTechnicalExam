using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameBoard
{
    #region Variables

    private int height = 0;
    public int Height { get { return height; } }

    private int width = 0;
    public int Width { get { return width; } }
  
    private SC_Gem[,] allGems;
  //  public Gem[,] AllGems { get { return allGems; } }

    private int score = 0;
    public int Score 
    {
        get { return score; }
        set { score = value; }
    }

    private List<List<SC_Gem>> currentGroupMatches = new();
    public List<List<SC_Gem>> CurrentGroupMatches => currentGroupMatches;
    
    private List<SC_Gem> currentMatches = new();
    public List<SC_Gem> CurrentMatches => currentMatches;
    
    private List<SC_Gem> newlyCreatedBombs = new();
    public List<SC_Gem> NewlyCreatedBombs => newlyCreatedBombs;
    
    #endregion
    
    public GameBoard(int _Width, int _Height)
    {
        height = _Height;
        width = _Width;
        allGems = new SC_Gem[width, height];
    }
    public bool MatchesAt(Vector2Int _PositionToCheck, SC_Gem _GemToCheck)
    {
        if (_PositionToCheck.x > 1)
        {
            if (allGems[_PositionToCheck.x - 1, _PositionToCheck.y].type == _GemToCheck.type &&
                allGems[_PositionToCheck.x - 2, _PositionToCheck.y].type == _GemToCheck.type)
                return true;
        }

        if (_PositionToCheck.y > 1)
        {
            if (allGems[_PositionToCheck.x, _PositionToCheck.y - 1].type == _GemToCheck.type &&
                allGems[_PositionToCheck.x, _PositionToCheck.y - 2].type == _GemToCheck.type)
                return true;
        }

        return false;
    }

    public void SetGem(int _X, int _Y, SC_Gem _Gem)
    {
        allGems[_X, _Y] = _Gem;
    }
    public SC_Gem GetGem(int _X,int _Y)
    {
        if (_X < 0 || _X >= width || _Y < 0 || _Y >= height)
            return null;
        return allGems[_X, _Y];
    }

    public void FindAllMatches()
    {
        ClearMatches();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                SC_Gem currentGem = allGems[x, y];
                if (currentGem != null)
                {
                    if (x > 0 && x < width - 1)
                    {
                        SC_Gem leftGem = allGems[x - 1, y];
                        SC_Gem rightGem = allGems[x + 1, y];
                        //checking no empty spots
                        if (leftGem != null && rightGem != null)
                        {
                            //Match
                            if (leftGem.type == currentGem.type && rightGem.type == currentGem.type)
                            {
                                currentGem.isMatch = true;
                                leftGem.isMatch = true;
                                rightGem.isMatch = true;
                                AddMatchesToList(currentGem, leftGem, rightGem);
                            }
                        }
                    }

                    if (y > 0 && y < height - 1)
                    {
                        SC_Gem aboveGem = allGems[x, y - 1];
                        SC_Gem bellowGem = allGems[x, y + 1];
                        //checking no empty spots
                        if (aboveGem != null && bellowGem != null)
                        {
                            //Match
                            if (aboveGem.type == currentGem.type && bellowGem.type == currentGem.type)
                            {
                                currentGem.isMatch = true;
                                aboveGem.isMatch = true;
                                bellowGem.isMatch = true;
                                AddMatchesToList(currentGem, aboveGem, bellowGem);
                            }
                        }
                    }
                }
            }

        for (var index = 0; index < currentGroupMatches.Count; index++)
        {
            var groupMatch = currentGroupMatches[index];
            var scGems = groupMatch.Distinct().ToList();
            currentGroupMatches[index] = scGems;
        }

        foreach (var groupMatch in currentGroupMatches)
            foreach (var gem in groupMatch)
                currentMatches.Add(gem);
    }

    private void AddMatchesToList(params SC_Gem[] matches)
    {
        foreach (var groupMatch in currentGroupMatches)
        {
            foreach (var match in matches)
                if (groupMatch.Contains(match))
                {
                    groupMatch.AddRange(matches);
                    return;
                }
        }
        
        currentGroupMatches.Add(matches.ToList());
    }

    public bool CheckForBombs()
    {
        var hasBomb = false;
        foreach (var groupMatch in currentGroupMatches)
        {
            foreach (var gem in groupMatch)
            {
                int x = gem.posIndex.x;
                int y = gem.posIndex.y;

                hasBomb |= CheckBombArea(x, y);
                hasBomb |= CheckBombArea(x - 1, y);
                hasBomb |= CheckBombArea(x + 1, y);
                hasBomb |= CheckBombArea(x, y - 1);
                hasBomb |= CheckBombArea(x, y + 1);
            }
        }

        return hasBomb;

        bool CheckBombArea(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;

            var potentialBomb = allGems[x, y];
            if (potentialBomb == null || !potentialBomb.isBomb || (newlyCreatedBombs?.Contains(potentialBomb) ?? false))
                return false;

            MarkBombArea(new Vector2Int(x, y), potentialBomb.blastSize);
            return true;
        }
    }

    public void MarkBombArea(Vector2Int bombPos, int _BlastSize)
    {
        string _print = "";
        for (int x = bombPos.x - _BlastSize; x <= bombPos.x + _BlastSize; x++)
        {
            for (int y = bombPos.y - _BlastSize; y <= bombPos.y + _BlastSize; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height && allGems[x, y] != null)
                {
                    var gem = allGems[x, y];
                    var canExplode = gem.isBomb && !gem.isMatch;
                    gem.isMatch = true;
                    currentMatches.Add(gem);
                    if (canExplode)
                        MarkBombArea(new Vector2Int(x, y), gem.blastSize);
                }
            }
        }
        currentMatches = currentMatches.Distinct().ToList();
    }
    
    public void ClearMatches()
    {
        currentGroupMatches.Clear();
        newlyCreatedBombs.Clear();
        currentMatches.Clear();
    }
}

