using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_Gem : MonoBehaviour
{
    [HideInInspector]
    public Vector2Int posIndex;

    private Vector2 firstTouchPosition;
    private Vector2 finalTouchPosition;
    private bool mousePressed;
    private float swipeAngle = 0;
    private SC_Gem otherGem;

    public GlobalEnums.GemType type;
    public bool isBomb;
    public bool isMatch = false;
    private Vector2Int previousPos;
    public GameObject destroyEffect;
    public int scoreValue = 10;

    public int blastSize = 1;
    
    private SC_GameLogic ScGameLogic => SC_GameLogic.Instance;

    void Update()
    {
        if (Vector2.Distance(transform.position, posIndex) > 0.01f)
            transform.position = Vector2.Lerp(transform.position, posIndex, SC_GameVariables.Instance.gemSpeed * Time.deltaTime);
        else
        {
            transform.position = new Vector3(posIndex.x, posIndex.y, posIndex.y * 0.01f);
            ScGameLogic.SetGem(posIndex.x, posIndex.y, this);
        }
        if (mousePressed && Input.GetMouseButtonUp(0))
        {
            mousePressed = false;
            if (ScGameLogic.CurrentState == GlobalEnums.GameState.move)
            {
                finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                CalculateAngle();
            }
        }
    }

    public void SetupGem(Vector2Int _Position)
    {
        posIndex = _Position;
        isMatch = false;
    }

    private void OnMouseDown()
    {
        if (ScGameLogic.CurrentState == GlobalEnums.GameState.move)
        {
            firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePressed = true;
        }
    }

    private void CalculateAngle()
    {
        swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x);
        swipeAngle = swipeAngle * 180 / Mathf.PI;

        if (Vector3.Distance(firstTouchPosition, finalTouchPosition) > .5f)
            MovePieces();
    }

    private void MovePieces()
    {
        previousPos = posIndex;

        if (swipeAngle < 45 && swipeAngle > -45 && posIndex.x < SC_GameVariables.Instance.rowsSize - 1)
        {
            otherGem = ScGameLogic.GetGem(posIndex.x + 1, posIndex.y);
            otherGem.posIndex.x--;
            posIndex.x++;

        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && posIndex.y < SC_GameVariables.Instance.colsSize - 1)
        {
            otherGem = ScGameLogic.GetGem(posIndex.x, posIndex.y + 1);
            otherGem.posIndex.y--;
            posIndex.y++;
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && posIndex.y > 0)
        {
            otherGem = ScGameLogic.GetGem(posIndex.x, posIndex.y - 1);
            otherGem.posIndex.y++;
            posIndex.y--;
        }
        else if (swipeAngle > 135 || swipeAngle < -135 && posIndex.x > 0)
        {
            otherGem = ScGameLogic.GetGem(posIndex.x - 1, posIndex.y);
            otherGem.posIndex.x++;
            posIndex.x--;
        }

        ScGameLogic.SetGem(posIndex.x, posIndex.y, this);
        ScGameLogic.SetGem(otherGem.posIndex.x, otherGem.posIndex.y, otherGem);

        StartCoroutine(CheckMoveCo());
    }

    public IEnumerator CheckMoveCo()
    {
        ScGameLogic.SetState(GlobalEnums.GameState.wait);

        yield return new WaitForSeconds(.5f);
        ScGameLogic.FindAllMatches(this, otherGem);
        
        if (otherGem != null)
        {
            if (isMatch == false && otherGem.isMatch == false)
            {
                otherGem.posIndex = posIndex;
                posIndex = previousPos;

                ScGameLogic.SetGem(posIndex.x, posIndex.y, this);
                ScGameLogic.SetGem(otherGem.posIndex.x, otherGem.posIndex.y, otherGem);

                yield return new WaitForSeconds(.5f);
                ScGameLogic.SetState(GlobalEnums.GameState.move);
            }
            else
            {
                ScGameLogic.StartDestroyMatches(this, otherGem);
            }
        }
    }
}
