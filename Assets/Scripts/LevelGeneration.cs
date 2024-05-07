using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGeneration : MonoBehaviour
{
    [HideInInspector]
    public Vector2 worldSize = new Vector2(6, 6);
    [HideInInspector]
    public Room[,] rooms;
    [HideInInspector]
    public List<Vector2> takenPositions = new List<Vector2>();
    [HideInInspector]
    public int gridSizeX, gridSizeY, numberOfRooms = 30;
    public GameObject roomWhiteObj;
    public Transform mapRoot;

    public Room[,] GetRooms()
    {
        return rooms;
    }

    public Room GetSingleRoom(int x, int y)
    {
        // Check if the room is within the bounds of the array
        if (x < -gridSizeX || x > gridSizeX || y < -gridSizeY || y > gridSizeY * 2)
        {
            return null;
        }
        return rooms[x + gridSizeX, y + gridSizeY];
    }

    public int GetGridSizeX()
    {
        return gridSizeX;
    }

    public int GetGridSizeY()
    {
        return gridSizeY;
    }
    public void setWorldSize(float worldSize)
    {
        switch (worldSize)
        {
            case 1.0f: // Pair Room
                this.worldSize = new Vector2(3, 3);
                this.numberOfRooms = 2;
                break;
            case 2.0f: // Small Hospital
                this.worldSize = new Vector2(4, 4);
                this.numberOfRooms = 10;
                break;
            case 3.0f: // Medium Hospital
                this.worldSize = new Vector2(5, 5);
                this.numberOfRooms = 20;
                break;
            case 4.0f: // Large Hospital
                this.worldSize = new Vector2(6, 6);
                this.numberOfRooms = 30;
                break;
            default:
                Debug.Log("Invalid world size");
                return;
        }
    }

    public void Reset()
    {
        // Clear the map
        foreach (Transform child in mapRoot)
        {
            Destroy(child.gameObject);
        }
        // Clear the list of taken positions
        takenPositions.Clear();
        // Clear the array of rooms
        rooms = null;
    }
    public void CreateRooms()
    {
        //setup
        rooms = new Room[gridSizeX * 2, gridSizeY * 2];
        rooms[gridSizeX, gridSizeY] = new Room(Vector2.zero, 1); //create room in center of grid and room type 1 (center room)
        
        // If takenPositions is not empty, clear it
        if (takenPositions.Count > 0)
        {
            takenPositions.Clear();
        }
        takenPositions.Insert(0, Vector2.zero);
        string roomTag = "";
        Vector2 checkPos = Vector2.zero;
        // The higher the randomCompare, the more likely the room will branch out
        float randomCompare = 0.2f, randomCompareStart = 0.7f, randomCompareEnd = 0.5f;
        //add rooms
        for (int i = 0; i < numberOfRooms - 1; i++)
        {
            roomTag = "";
            // With more rooms, the lower the randonPerc, wich makes randomCompare higher, the less likely to enter the if statement.
            // Not entering the SelectiveNewPosition() method means that the room will not branch out.
            float randomPerc = ((float)i) / (((float)numberOfRooms - 1));
            // Lerp: return a + (b - a) * Clamp01(t); t is clamped between 0 and 1
            randomCompare = Mathf.Lerp(randomCompareStart, randomCompareEnd, randomPerc);

            //grab new position
            checkPos = NewPosition();

            // If its a case where the room has more than one neighbor, we want to make it more likely to branch out based on the randomCompare

            // If the room is the last one, make it tag 3 (SPDs)
            if (((float)i) == numberOfRooms - 2)
            {
                checkPos = LonelyNewPosition();
                roomTag = "SPD";
            }
            else if (((float)i) / ((float)numberOfRooms - 1) > 0.7f)
            {
                checkPos = LonelyNewPosition();
            }
            else if (NumberOfNeighbors(checkPos, takenPositions) > 1 && UnityEngine.Random.value > randomCompare)
            {
                int iterations = 0;
                do
                {   
                    // Select a new position where the starting position changes, having only one neighbor
                    checkPos = SelectiveNewPosition();
                    iterations++;
                } while (NumberOfNeighbors(checkPos, takenPositions) > 1 && iterations < 100);
                if (iterations >= 50)
                    print("error: could not create with fewer neighbors than : " + NumberOfNeighbors(checkPos, takenPositions));
            }
            //finalize position
            if (roomTag == "SPD")
            {
                rooms[(int)checkPos.x + gridSizeX, (int)checkPos.y + gridSizeY] = new Room(checkPos, 3);
            }
            else
            {
                rooms[(int)checkPos.x + gridSizeX, (int)checkPos.y + gridSizeY] = new Room(checkPos, 0);
            }
            takenPositions.Insert(0, checkPos);
        }

    }
    Vector2 NewPosition()
    {
        int x = 0, y = 0;
        Vector2 checkingPos = Vector2.zero;
        do
        {
            int index = Mathf.RoundToInt(UnityEngine.Random.value * (takenPositions.Count - 1)); // pick a random room
            x = (int)takenPositions[index].x;//capture its x, y position
            y = (int)takenPositions[index].y;
            bool UpDown = (UnityEngine.Random.value < 0.5f);//randomly pick wether to look on hor or vert axis
            bool positive = (UnityEngine.Random.value < 0.5f);//pick whether to be positive or negative on that axis
            if (UpDown)
            { //find the position based on the above bools
                if (positive)
                {
                    y += 1;
                }
                else
                {
                    y -= 1;
                }
            }
            else
            {
                if (positive)
                {
                    x += 1;
                }
                else
                {
                    x -= 1;
                }
            }
            checkingPos = new Vector2(x, y);
        } while (takenPositions.Contains(checkingPos) || x >= gridSizeX || x < -gridSizeX || y >= gridSizeY || y < -gridSizeY); //make sure the position is valid
        return checkingPos;
    }
    Vector2 SelectiveNewPosition()
    { // method differs from the above in the two commented ways
        int index = 0, inc = 0;
        int x = 0, y = 0;
        Vector2 checkingPos = Vector2.zero;
        do
        {
            inc = 0;
            do
            {
                //instead of getting a room to find an adject empty space, we start with one that only 
                //has one neighbor. This will make it more likely that it returns a room that branches out
                index = Mathf.RoundToInt(UnityEngine.Random.value * (takenPositions.Count - 1));
                inc++;
            } while (NumberOfNeighbors(takenPositions[index], takenPositions) > 1 && inc < 100);
            x = (int)takenPositions[index].x;
            y = (int)takenPositions[index].y;
            bool UpDown = (UnityEngine.Random.value < 0.5f);
            bool positive = (UnityEngine.Random.value < 0.5f);
            if (UpDown)
            {
                if (positive)
                {
                    y += 1;
                }
                else
                {
                    y -= 1;
                }
            }
            else
            {
                if (positive)
                {
                    x += 1;
                }
                else
                {
                    x -= 1;
                }
            }
            checkingPos = new Vector2(x, y);
        } while (takenPositions.Contains(checkingPos) || x >= gridSizeX || x < -gridSizeX || y >= gridSizeY || y < -gridSizeY);
        if (inc >= 100)
        { // break loop if it takes too long: this loop isnt garuanteed to find solution, which is fine for this
            print("Error: could not find position with only one neighbor");
        }
        return checkingPos;
    }

    Vector2 LonelyNewPosition()
    { // Creates a room that is connect to another room by only one door
        int x = 0, y = 0;
        Vector2 checkingPos = Vector2.zero;
        int inc = 0;
        do
        {
            inc++;
            int index = Mathf.RoundToInt(UnityEngine.Random.value * (takenPositions.Count - 1)); // pick a random room
            x = (int)takenPositions[index].x;//capture its x, y position
            y = (int)takenPositions[index].y;
            bool UpDown = (UnityEngine.Random.value < 0.5f);//randomly pick wether to look on hor or vert axis
            bool positive = (UnityEngine.Random.value < 0.5f);//pick whether to be positive or negative on that axis
            if (UpDown)
            { //find the position bnased on the above bools
                if (positive)
                {
                    y += 1;
                }
                else
                {
                    y -= 1;
                }
            }
            else
            {
                if (positive)
                {
                    x += 1;
                }
                else
                {
                    x -= 1;
                }
            }
            checkingPos = new Vector2(x, y);

            // Stop loop when new position does not have more than one neighbor
        } while ((takenPositions.Contains(checkingPos)) || (NumberOfNeighbors(checkingPos, takenPositions) > 1) && (inc < 100) || x >= gridSizeX || x < -gridSizeX || y >= gridSizeY || y < -gridSizeY); //make sure the position is valid
        // Debug.Log((bool)(((takenPositions.Contains(checkingPos)) || (NumberOfNeighbors(checkingPos, takenPositions) > 1) || (inc < 100))));


        if (inc >= 100)
        { // break loop if it takes too long: this loop isnt garuanteed to find solution, which is fine for this
            Debug.Log("Error: could not find position with only one neighbor");
        }
        // Print the number of neighbors of chosen position
        // Debug.Log("Number of neighbors: " + (NumberOfNeighbors(checkingPos, takenPositions)));
        // Debug.Log("takenPositions.Contains: " + takenPositions.Contains(checkingPos));
        // Debug.Log("(inc < 100): " + (bool)(inc < 100));
        return checkingPos;
    }

    public int NumberOfNeighbors(Vector2 checkingPos, List<Vector2> usedPositions)
    {
        int ret = 0; // start at zero, add 1 for each side there is already a room
        if (usedPositions.Contains(checkingPos + Vector2.right))
        { //using Vector.[direction] as short hands, for simplicity
            ret++;
        }
        if (usedPositions.Contains(checkingPos + Vector2.left))
        {
            ret++;
        }
        if (usedPositions.Contains(checkingPos + Vector2.up))
        {
            ret++;
        }
        if (usedPositions.Contains(checkingPos + Vector2.down))
        {
            ret++;
        }
        return ret;
    }
    public void DrawMap()
    // This function will draw a debugging map outside of the simulation bounds
    {
        foreach (Room room in rooms)
        {
            if (room == null)
            {
                continue; //skip where there is no room
            }
            Vector2 drawPos = room.gridPos;
            drawPos.x *= 16;//aspect ratio of map sprite
            drawPos.y *= 8;
            // Debug.Log("drawPos: " + drawPos);   
            // Debug.Log("x: " + drawPos.x.ToString() + "   y: " + drawPos.y.ToString());
            //create map obj and assign its variables
            MapSpriteSelector mapper = UnityEngine.Object.Instantiate(roomWhiteObj, drawPos, Quaternion.identity).GetComponent<MapSpriteSelector>();
            mapper.type = room.type;
            mapper.up = room.doorTop;
            mapper.down = room.doorBot;
            mapper.right = room.doorRight;
            mapper.left = room.doorLeft;
            mapper.gameObject.transform.parent = mapRoot;
            //Debug.Log("mapper position: " + mapper.transform.position);
        }
        // Set the position of the map to be out of bounds
        mapRoot.transform.position = new Vector3(55, 750, 15);
    }
    public void SetRoomDoors()
    {
        for (int x = 0; x < ((gridSizeX * 2)); x++)
        {
            for (int y = 0; y < ((gridSizeY * 2)); y++)
            {
                if (rooms[x, y] == null)
                {
                    continue;
                }
                Vector2 gridPosition = new Vector2(x, y);
                if (y - 1 < 0)
                { //check above
                    rooms[x, y].doorBot = false;
                }
                else
                {
                    rooms[x, y].doorBot = (rooms[x, y - 1] != null);
                }
                if (y + 1 >= gridSizeY * 2)
                { //check bellow
                    rooms[x, y].doorTop = false;
                }
                else
                {
                    rooms[x, y].doorTop = (rooms[x, y + 1] != null);
                }
                if (x - 1 < 0)
                { //check left
                    rooms[x, y].doorLeft = false;
                }
                else
                {
                    rooms[x, y].doorLeft = (rooms[x - 1, y] != null);
                }
                if (x + 1 >= gridSizeX * 2)
                { //check right
                    rooms[x, y].doorRight = false;
                }
                else
                {
                    rooms[x, y].doorRight = (rooms[x + 1, y] != null);
                }
            }
        }
    }
}