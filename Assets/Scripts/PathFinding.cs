using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class Pathfinding
{

    private const int MOVE_STRAIGHT = 10;
    private const int MOVE_DIAGONAL = 14;

    private LevelGeneration levelGeneration;
    private Room[,] grid;
    private List<Room> openList;
    private List<Room> closedList;

    private void Start()
    {
        levelGeneration = GameObject.Find("LevelGeneration").GetComponent<LevelGeneration>();
        grid = levelGeneration.GetRooms();
    }

    public List<Room> FindPath(Vector2 startGridPos, Vector2 EndGridPos)
    {
        // Get the start and end nodes based on the room grid positions
        Room startNode = levelGeneration.GetSingleRoom((int)startGridPos.x, (int)startGridPos.y);
        Room endNode = levelGeneration.GetSingleRoom((int)EndGridPos.x, (int)EndGridPos.y);

        openList = new List<Room> { startNode };
        closedList = new List<Room>();

        for (int x = 0; x < levelGeneration.GetGridSizeX(); x++)
        {
            for (int y = 0; y < levelGeneration.GetGridSizeY(); y++)
            {
                Room pathNode = levelGeneration.GetSingleRoom(x,y);
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode.prevNode = null;
            }
        }

        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();

        while (openList.Count > 0)
        {
            Room currentNode = GetLowestFCostNode(openList);
            if (currentNode == endNode)
            {
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (Room neighborNode in GetNeighborList(currentNode))
            {
                if (closedList.Contains(neighborNode)) continue;

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighborNode);
                if (tentativeGCost < neighborNode.gCost)
                {
                    neighborNode.prevNode = currentNode;
                    neighborNode.gCost = tentativeGCost;
                    neighborNode.hCost = CalculateDistanceCost(neighborNode, endNode);
                    neighborNode.CalculateFCost();

                    if (!openList.Contains(neighborNode))
                    {
                        openList.Add(neighborNode);
                    }
                }
            }
        }

        //No Path
        return null;
    }

    private List<Room> GetNeighborList(Room currentNode)
    {
        List<Room> neighborList = new List<Room>();

        if (currentNode.gridPos.x - 1 >= 0)
        {
            neighborList.Add(GetNode((int)currentNode.gridPos.x - 1, (int)currentNode.gridPos.y)); //Left
            if ((int)currentNode.gridPos.y - 1 >= 0) neighborList.Add(GetNode((int)currentNode.gridPos.x - 1, (int)currentNode.gridPos.y - 1)); //Left Down
            if ((int)currentNode.gridPos.y + 1 < levelGeneration.GetGridSizeY()) neighborList.Add(GetNode((int)currentNode.gridPos.x - 1, (int)currentNode.gridPos.y + 1)); //Left Up
        }
        if ((int)currentNode.gridPos.x + 1 < levelGeneration.GetGridSizeY())
        {
            neighborList.Add(GetNode((int)currentNode.gridPos.x + 1, (int)currentNode.gridPos.y)); //Right
            if ((int)currentNode.gridPos.y - 1 >= 0) neighborList.Add(GetNode((int)currentNode.gridPos.x + 1, (int)currentNode.gridPos.y - 1)); //Right Down
            if ((int)currentNode.gridPos.y + 1 < levelGeneration.GetGridSizeY()) neighborList.Add(GetNode((int)currentNode.gridPos.x + 1, (int)currentNode.gridPos.y + 1)); //Right Up
        }
        if ((int)currentNode.gridPos.y - 1 >= 0) neighborList.Add(GetNode((int)currentNode.gridPos.x, (int)currentNode.gridPos.y - 1)); //Down
        if ((int)currentNode.gridPos.y + 1 < levelGeneration.GetGridSizeY()) neighborList.Add(GetNode((int)currentNode.gridPos.x, (int)currentNode.gridPos.y + 1)); //Up

        return neighborList;
    }

    private Room GetNode(int x, int y)
    {
        return levelGeneration.GetSingleRoom(x,y);
    }

    private List<Room> CalculatePath(Room endNode)
    {
        List<Room> path = new List<Room>();
        path.Add(endNode);
        Room currentNode = endNode;
        while (currentNode.prevNode != null)
        {
            path.Add(currentNode.prevNode);
            currentNode = currentNode.prevNode;
        }
        path.Reverse();
        return path;
    }

    private int CalculateDistanceCost(Room a, Room b)
    {
        int xDistance = Mathf.Abs((int)a.gridPos.x - (int)b.gridPos.x);
        int yDistance = Mathf.Abs((int)a.gridPos.y - (int)b.gridPos.y);
        int remaining = Mathf.Abs(xDistance - yDistance);
        return MOVE_DIAGONAL * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT * remaining;
    }

    private Room GetLowestFCostNode(List<Room> pathNodeList)
    {
        Room lowestFCostNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }
}