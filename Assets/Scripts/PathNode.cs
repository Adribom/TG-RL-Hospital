using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode
{
    private Room room;
    public int x;
    public int y;

    public int gCost;
    public int hCost;
    public int fCost;

    public bool isWalkable;
    public bool hasWalls;
    public PathNode prevNode;

    public PathNode(Room room, int x, int y)
    {
        this.room = room;
        this.x = x;
        this.y = y;
        isWalkable = true;
        hasWalls = false;
    }

    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }

    public override string ToString()
    {
        return x + "," + y;
    }
}
