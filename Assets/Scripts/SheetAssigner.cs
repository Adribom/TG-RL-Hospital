using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheetAssigner : MonoBehaviour
{
    [SerializeField]
    Texture2D[] sheetsNormal;
    [SerializeField]
    Texture2D[] sheetsOR;
    [SerializeField]
    Texture2D[] sheetsCS;
    [SerializeField]
    Texture2D[] sheetsSPD;
    [SerializeField]
    Texture2D[] sheetsCorridor;
    [SerializeField]
    Texture2D[] sheetsLessonTwo;
    [SerializeField]
    GameObject RoomObj;

    // roomDimensions without overlaping: 16*17, 16*9
    public Vector2 roomDimensions = new Vector2(255, 128);

    // Original gutterSize: 16*9, 16*4
    public Vector2 gutterSize = new Vector2(0, 0);

    // Room Tag
    public string roomTag = "";

    private bool removeWalls = false;

    public void Assign(Room[,] rooms, List<Vector2> takenPositions)
    {
        foreach (Room room in rooms)
        {
            //Variable to hold the texture array
            Texture2D[] currentSheets;
            roomTag = "";
            removeWalls = false;

            // if else statement to choose the correct texture array
            if (room == null)
            {
                //skip point where there is no room
                continue;
            }
            else if (takenPositions.Count == 1)
            {
                // Use OR templates for the case where there is only one room
                currentSheets = sheetsOR;

                // Change the type of the room to 2 (OR) and set tag
                room.type = 2;
                roomTag = "OR";
            }
            else if (room.type == 1)
            {
                // Central room is always a Central Storage (CS)
                currentSheets = sheetsCS;
                roomTag = "CS";
            }
            else if (room.type == 3)
            {
                // Sterile Processing Department (SPD) is always the last room
                currentSheets = sheetsSPD;
                roomTag = "SPD";
            }
            else if (GetComponent<LevelGeneration>().NumberOfNeighbors(room.gridPos, takenPositions) == 1)
            {
                // Use OR templates for rooms with only one neighbor
                currentSheets = sheetsOR;

                // Change the type of the room to 2 (OR) and set tag
                room.type = 2;
                roomTag = "OR";
            }
            else if (GetComponent<LevelGeneration>().NumberOfNeighbors(room.gridPos, takenPositions) == 4)
            {
                // Use corridor templates for rooms with four neighbors (without walls or doors)
                currentSheets = sheetsCorridor;
                room.hasWalls = false;
            }
            else if (takenPositions.Count == 4)
            {
                // Special case for second lesson on curriculum
                currentSheets = sheetsLessonTwo;
            }
            else
            {
                // Use normal templates for rooms with more than one neighbor
                currentSheets = sheetsNormal;
            }

            if (takenPositions.Count == 4)
            {
                // Special case for second lesson on curriculum
                removeWalls = true;
                room.hasWalls = false;
            }

            //pick a random index for the array
            int index = Mathf.RoundToInt(Random.value * (currentSheets.Length - 1));
            //find position to place room
            Vector2 xzPos = GridPosToWorldPos(room.gridPos);
            Vector3 pos = new Vector3(xzPos.x, transform.position.y, xzPos.y);
            RoomInstance myRoom = Instantiate(RoomObj, pos, Quaternion.identity, GetComponent<LevelGeneration>().transform).GetComponent<RoomInstance>();
            myRoom.Setup(currentSheets[index], room.gridPos, room.type, room.doorTop, room.doorBot, room.doorLeft, room.doorRight, GetComponent<LevelGeneration>().NumberOfNeighbors(room.gridPos, takenPositions), removeWalls);
            if (roomTag != "")
            {
                myRoom.tag = roomTag;
            }
            myRoom.transform.Rotate(90, 0, 0); // Rotate each room 90 degrees on the x-axis to make the rooms face upwards
        }
    }

    public Vector2 WorldPosToGridPos(Transform transform)
    {
        Vector2 gridPos = new Vector2(Mathf.RoundToInt(transform.position.x / (roomDimensions.x + gutterSize.x)), Mathf.RoundToInt(transform.position.z / (roomDimensions.y + gutterSize.y)));
        return gridPos;
    }

    public Vector2 GridPosToWorldPos(Vector2 gridPos)
    {
        Vector2 position = new Vector2(gridPos.x * (roomDimensions.x + gutterSize.x), gridPos.y * (roomDimensions.y + gutterSize.y));
        return position;
    }
}
