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

    public void FindAllMatches(SC_Gem gem1 = null, SC_Gem gem2 = null)
    {
        ClearMatches();
        CheckBombMatch(gem1, gem2);

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            SC_Gem currentGem = allGems[x, y];
            if (currentGem == null) continue;

            CheckLineMatch(currentGem, x, y, 1, 0);   // Horizontal
            CheckLineMatch(currentGem, x, y, 0, 1);   // Vertical
        }

        // Remove duplicates in each group
        for (int i = 0; i < currentGroupMatches.Count; i++)
            currentGroupMatches[i] = currentGroupMatches[i].Distinct().ToList();

        // Add all to final match list
        foreach (var group in currentGroupMatches)
        foreach (var gem in group)
            currentMatches.Add(gem);
    }
    
    private void CheckBombMatch(SC_Gem centerGem, SC_Gem other)
    {
        if (centerGem == null || !centerGem.isBomb || other == null || !other.isBomb) 
            return;
        
        AddMatchesToList(centerGem);
        AddMatchesToList(other);
    }

    private void CheckLineMatch(SC_Gem centerGem, int x, int y, int dx, int dy)
    {
        int leftX = x - dx;
        int leftY = y - dy;
        int rightX = x + dx;
        int rightY = y + dy;

        if (!IsInside(leftX, leftY) || !IsInside(rightX, rightY))
            return;

        SC_Gem gemA = allGems[leftX, leftY];
        SC_Gem gemB = allGems[rightX, rightY];

        // Normal 3-match
        if (CheckMatchesOfType(centerGem.type, gemA, gemB))
            AddMatchesToList(centerGem, gemA, gemB);
    }
    
    private bool IsInside(int x, int y)
        => x >= 0 && x < width && y >= 0 && y < height;
    
    private bool CheckMatchesOfType(GlobalEnums.GemType type, params SC_Gem[] gems)
        => gems.All(g => g != null && g.type == type);

    private void AddMatchesToList(params SC_Gem[] matches)
    {
        foreach (var g in matches)
            g.isMatch = true;

        foreach (var group in currentGroupMatches)
        {
            if (matches.Any(m => group.Contains(m)))
            {
                group.AddRange(matches);
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

                hasBomb |= CheckBombArea(x, y, true);
                hasBomb |= CheckBombArea(x - 1, y);
                hasBomb |= CheckBombArea(x + 1, y);
                hasBomb |= CheckBombArea(x, y - 1);
                hasBomb |= CheckBombArea(x, y + 1);
            }
        }

        return hasBomb;

        bool CheckBombArea(int x, int y, bool destroySpecialBomb = false)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;

            var potentialBomb = allGems[x, y];
            if (potentialBomb == null || !potentialBomb.isBomb || (newlyCreatedBombs?.Contains(potentialBomb) ?? false))
                return false;
            if(!destroySpecialBomb && potentialBomb.type != GlobalEnums.GemType.normalBomb)
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

