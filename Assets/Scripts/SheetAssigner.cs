using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheetAssigner : MonoBehaviour {
	[SerializeField]
	Texture2D[] sheetsNormal;
	[SerializeField]
	GameObject RoomObj;
	// roomDimensions without overlaping: 16*17, 16*9
	public Vector2 roomDimensions = new Vector2(255, 128);

	// Original gutterSize: 16*9, 16*4
	public Vector2 gutterSize = new Vector2(0,0);
	public void Assign(Room[,] rooms){
		foreach (Room room in rooms){
			//skip point where there is no room
			if (room == null){
				continue;
			}
			//pick a random index for the array
			int index = Mathf.RoundToInt(Random.value * (sheetsNormal.Length -1));
			//find position to place room
			Vector3 pos = new Vector3(room.gridPos.x * (roomDimensions.x + gutterSize.x), 0, room.gridPos.y * (roomDimensions.y + gutterSize.y));
			RoomInstance myRoom = Instantiate(RoomObj, pos, Quaternion.identity).GetComponent<RoomInstance>();
			myRoom.Setup(sheetsNormal[index], room.gridPos, room.type, room.doorTop, room.doorBot, room.doorLeft, room.doorRight);
			myRoom.transform.Rotate(90, 0, 0); // Rotate each room 90 degrees on the x-axis to make the rooms face upwards
		}
	}
}
