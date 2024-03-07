using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    public Texture2D tex;
    [HideInInspector]
    public Vector2 gridPos;
    public int type; // 0: normal, 1: center
    [HideInInspector]
    public bool doorTop, doorBot, doorLeft, doorRight;
    [SerializeField]
    GameObject doorU, doorD, doorL, doorR, doorWall;
    [SerializeField]
    ColorToGameObject[] mappings;
    float tileSize = 16;
    Vector2 roomSizeInTiles = new Vector2(9, 17); // Vector2(x, y), where x is the width and y is the height of the room
    public void Setup(Texture2D _tex, Vector2 _gridPos, int _type, bool _doorTop, bool _doorBot, bool _doorLeft, bool _doorRight, int numNeighbors)
    {
        tex = _tex;
        gridPos = _gridPos;
        type = _type;
        doorTop = _doorTop;
        doorBot = _doorBot;
        doorLeft = _doorLeft;
        doorRight = _doorRight;
        if (numNeighbors == 4 && type == 0) 
        {
            GenerateRoomTiles(numNeighbors, type);
        }
        else
        {
            MakeDoors();
            GenerateRoomTiles(numNeighbors, type);

        }
    }
    void MakeDoors()
    {
        //top door, get position and set rotation then spawn
        Vector3 spawnPos = transform.position + Vector3.up * (roomSizeInTiles.y / 4 * tileSize) - Vector3.up * (tileSize / 4);
        Vector3 rotation = new Vector3(0, 0, 90);
        PlaceDoor(spawnPos, doorTop, doorU, rotation);
        //bottom door
        spawnPos = transform.position + Vector3.down * (roomSizeInTiles.y / 4 * tileSize) - Vector3.down * (tileSize / 4);
        rotation = new Vector3(0, 0, 90);
        PlaceDoor(spawnPos, doorBot, doorD, rotation);
        //right door
        spawnPos = transform.position + Vector3.right * (roomSizeInTiles.x * tileSize) - Vector3.right * (tileSize);
        rotation = new Vector3(0, 0, 0);
        PlaceDoor(spawnPos, doorRight, doorR, rotation);
        //left door
        spawnPos = transform.position + Vector3.left * (roomSizeInTiles.x * tileSize) - Vector3.left * (tileSize);
        rotation = new Vector3(0, 0, 0);
        PlaceDoor(spawnPos, doorLeft, doorL, rotation);
    }
    void PlaceDoor(Vector3 spawnPos, bool door, GameObject doorSpawn, Vector3 rotation)
    {
        // check whether its a door or wall, then spawn
        if (door)
        {
            Instantiate(doorSpawn, spawnPos, Quaternion.Euler(rotation)).transform.parent = transform;
        }
        else
        {
            Instantiate(doorWall, spawnPos, Quaternion.Euler(rotation)).transform.parent = transform;
        }
    }
    void GenerateRoomTiles(int numNeighbors, int type)
    {
        //Variables for iteration
        int xStart = 0;
        int yStart = 0;
        //Variables for tile size
        int width = tex.width;
        int height = tex.height;

        //if the room has 4 neighbors, don't consider the pixels on the edges.
        //This creates a room without walls or doors, only with obstacles
        if (numNeighbors == 4 && type == 0)
        {
            xStart++;
            yStart++;
            width -= 1;
            height -= 1;
        }

        //loop through every pixel of the texture
        for (int x = xStart; x < width; x++)
        {
            for (int y = yStart; y < height; y++)
            {
                GenerateTile(x, y);
            }
        }
    }
    void GenerateTile(int x, int y)
    {
        Color pixelColor = tex.GetPixel(x, y);
        //skip clear spaces in texture
        if (pixelColor.a == 0)
        {
            return;
        }
        //find the color to math the pixel
        foreach (ColorToGameObject mapping in mappings)
        {
            if (mapping.color.Equals(pixelColor))
            {
                // Debug.Log(mapping.color + ", " + pixelColor);   
                Vector3 spawnPos = positionFromTileGrid(x, y);
                Instantiate(mapping.prefab, spawnPos, Quaternion.identity).transform.parent = this.transform;
            }
            else
            {
                //forgot to remove the old print for the tutorial lol so I'll leave it here too
                //print(mapping.color + ", " + pixelColor);
            }
        }
    }
    Vector3 positionFromTileGrid(int x, int y)
    {
        Vector3 ret;
        //find difference between the corner of the texture and the center of this object
        Vector3 offset = new Vector3((-roomSizeInTiles.x + 1) * tileSize, (roomSizeInTiles.y / 4) * tileSize - (tileSize / 4), 0);
        //find scaled up position at the offset
        ret = new Vector3(tileSize * (float)x, -tileSize * (float)y, 0) + offset + transform.position;
        return ret;
    }
}
