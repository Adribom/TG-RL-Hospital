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
    GameObject RoomObj;

    // roomDimensions without overlaping: 16*17, 16*9
    public Vector2 roomDimensions = new Vector2(255, 128);

    // Original gutterSize: 16*9, 16*4
    public Vector2 gutterSize = new Vector2(0, 0);

    // Room Tag
    public string roomTag = "";

    public void Assign(Room[,] rooms, List<Vector2> takenPositions)
    {
        foreach (Room room in rooms)
        {
            //Variable to hold the texture array
            Texture2D[] currentSheets;
            roomTag = "";

            // if else statement to choose the correct texture array
            if (room == null)
            {
                //skip point where there is no room
                continue;
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
            }
            else
            {
                // Use normal templates for rooms with more than one neighbor
                currentSheets = sheetsNormal;
            }

            //pick a random index for the array
            int index = Mathf.RoundToInt(Random.value * (currentSheets.Length - 1));
            //find position to place room
            Vector3 pos = new Vector3(room.gridPos.x * (roomDimensions.x + gutterSize.x), transform.position.y, room.gridPos.y * (roomDimensions.y + gutterSize.y));
            RoomInstance myRoom = Instantiate(RoomObj, pos, Quaternion.identity, GetComponent<LevelGeneration>().transform).GetComponent<RoomInstance>();
            myRoom.Setup(currentSheets[index], room.gridPos, room.type, room.doorTop, room.doorBot, room.doorLeft, room.doorRight, GetComponent<LevelGeneration>().NumberOfNeighbors(room.gridPos, takenPositions));
            if (roomTag != "")
            {
                myRoom.tag = roomTag;
            }
            myRoom.transform.Rotate(90, 0, 0); // Rotate each room 90 degrees on the x-axis to make the rooms face upwards
        }
    }
}
