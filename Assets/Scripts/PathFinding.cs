using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class PathFinding : MonoBehaviour
{

    private const int MOVE_STRAIGHT = 10;
    private const int MOVE_DIAGONAL = 14;

    private LevelGeneration levelGeneration;
    private List<Room> openList;
    private List<Room> closedList;

    private void Start()
    {
        levelGeneration = GameObject.Find("LevelGenerator").GetComponent<LevelGeneration>();
    }

    public List<Room> FindPath(Vector2 startGridPos, Vector2 EndGridPos)
    {
        levelGeneration = GameObject.Find("LevelGenerator").GetComponent<LevelGeneration>();
        // Get the start and end nodes based on the room grid positions
        Room startNode = levelGeneration.GetSingleRoom((int)startGridPos.x, (int)startGridPos.y);
        Room endNode = levelGeneration.GetSingleRoom((int)EndGridPos.x, (int)EndGridPos.y);
        if (startNode == null || endNode == null) { return null; }

        openList = new List<Room> { startNode };
        closedList = new List<Room>();

        for (int x = -levelGeneration.GetGridSizeX(); x < levelGeneration.GetGridSizeX(); x++)
        {
            for (int y = -levelGeneration.GetGridSizeX(); y < levelGeneration.GetGridSizeX(); y++)
            {
                Room pathNode = levelGeneration.GetSingleRoom(x,y);
                if (pathNode != null) 
                {
                    pathNode.gCost = int.MaxValue;
                    pathNode.CalculateFCost();
                    pathNode.prevNode = null;
                }
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

        if (currentNode.hasWalls)
        {
            if (currentNode.doorTop) neighborList.Add(GetNode((int)currentNode.gridPos.x, (int)currentNode.gridPos.y + 1)); //Up
            if (currentNode.doorBot) neighborList.Add(GetNode((int)currentNode.gridPos.x, (int)currentNode.gridPos.y - 1)); //Down
            if (currentNode.doorLeft) neighborList.Add(GetNode((int)currentNode.gridPos.x - 1, (int)currentNode.gridPos.y)); //Left
            if (currentNode.doorRight) neighborList.Add(GetNode((int)currentNode.gridPos.x + 1, (int)currentNode.gridPos.y)); //Right
        }
        else
        {
            List<bool> noWallDiagonalRooms = new List<bool> { true, true, true, true }; // leftDown, rightDown, leftUp, rightUp

            int x = (int)currentNode.gridPos.x;
            int y = (int)currentNode.gridPos.y;

            Room up = GetNode(x, y + 1);
            Room down = GetNode(x, y - 1);
            Room left = GetNode(x - 1, y);
            Room right = GetNode(x + 1, y);

            if (up != null) { neighborList.Add(up); } else { 
                noWallDiagonalRooms[2] = false; // leftUp
                noWallDiagonalRooms[3] = false; // rightUp
            }
            if (down != null) { neighborList.Add(down); } else {
                noWallDiagonalRooms[0] = false; // leftDown
                noWallDiagonalRooms[1] = false; // rightDown
            }
            if (left != null) { neighborList.Add(left); } else {
                noWallDiagonalRooms[0] = false; // leftDown
                noWallDiagonalRooms[2] = false; // leftUp
            }
            if (right != null) { neighborList.Add(right); } else {
                noWallDiagonalRooms[1] = false; // rightDown    
                noWallDiagonalRooms[3] = false; // rightUp
            }

            Room leftDown = GetNode(x - 1, y - 1);
            Room rightDown = GetNode(x + 1, y - 1);
            Room leftUp = GetNode(x - 1, y + 1);
            Room rightUp = GetNode(x + 1, y + 1);
            for (int i = 0; i < noWallDiagonalRooms.Count; i++)
            {

                if (noWallDiagonalRooms[i])
                {
                    switch (i)
                    {
                        case 0:
                            if (leftDown != null && (!left.hasWalls || !down.hasWalls)) 
                            { 
                                neighborList.Add(GetNode(x - 1, y - 1)); // leftDown
                            } 
                            break;
                        case 1:
                            if (rightDown != null && (!right.hasWalls || !down.hasWalls))
                            {
                                neighborList.Add(GetNode(x + 1, y - 1)); // rightDown
                            }
                            break;
                        case 2:
                            if (leftUp != null && (!left.hasWalls || !up.hasWalls))
                            { 
                                neighborList.Add(GetNode(x - 1, y + 1)); // leftUp
                            } 
                            break;
                        case 3:
                            if (rightUp != null && (!right.hasWalls || !up.hasWalls))
                            {
                                neighborList.Add(GetNode(x + 1, y + 1)); // rightUp
                            } 
                            break;
                    }
                }
            }
        }
        return neighborList;
    }

    private Room GetNode(int x, int y)
    {
        //TODO: adicionar verificação para caso de entrada estar fora do index das salas
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