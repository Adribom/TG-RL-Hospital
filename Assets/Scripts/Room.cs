using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public Vector2 gridPos;
    public int type;
    public bool doorTop, doorBot, doorLeft, doorRight;

    // Atributes for A* algorithm
    public int gCost;
    public int hCost;
    public int fCost;

    public bool hasWalls;
    public Room prevNode;

    public Room(Vector2 _gridPos, int _type)
    {
        gridPos = _gridPos;
        type = _type; // 0: normal, 1: center room, 2: ORs, 3: SPDs
        hasWalls = false;
    }

    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }

    public override string ToString()
    {
        return gridPos.x + "," + gridPos.y;
    }
}