using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
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
    public float tileSize = 16;
    Vector2 roomSizeInTiles = new Vector2(9, 17); // Vector2(x, y), where x is the width and y is the height of the room
    public void Setup(Texture2D _tex, Vector2 _gridPos, int _type, bool _doorTop, bool _doorBot, bool _doorLeft, bool _doorRight, int numNeighbors, bool removeWalls)
    {
        tex = _tex;
        gridPos = _gridPos;
        type = _type;
        doorTop = _doorTop;
        doorBot = _doorBot;
        doorLeft = _doorLeft;
        doorRight = _doorRight;
        if ((numNeighbors == 4 && type == 0) || removeWalls) 
        {
            GenerateRoomTiles(numNeighbors, type, removeWalls);
        }
        else
        {
            MakeDoors();
            GenerateRoomTiles(numNeighbors, type, removeWalls);
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

    void PlaceWallsOnDoors(bool upperWall, bool bottomWall, bool leftWall, bool rightWall)
    {
        Vector3 spawnPos = Vector3.zero;
        Vector3 rotation = Vector3.zero;

        if (upperWall)
        {
            //top door, get position and set rotation then spawn
            spawnPos = transform.position + Vector3.up * (roomSizeInTiles.y / 4 * tileSize) - Vector3.up * (tileSize / 4);
            rotation = new Vector3(0, 0, 90);
            PlaceDoor(spawnPos, false, doorU, rotation);
        }
        if (bottomWall)
        {
            //bottom door
            spawnPos = transform.position + Vector3.down * (roomSizeInTiles.y / 4 * tileSize) - Vector3.down * (tileSize / 4);
            rotation = new Vector3(0, 0, 90);
            PlaceDoor(spawnPos, false, doorD, rotation);
        }
        if (rightWall)
        {
        //right door
        spawnPos = transform.position + Vector3.right * (roomSizeInTiles.x * tileSize) - Vector3.right * (tileSize);
        rotation = new Vector3(0, 0, 0);
        PlaceDoor(spawnPos, false, doorR, rotation);
        }
        if (leftWall)
        {
        //left door
        spawnPos = transform.position + Vector3.left * (roomSizeInTiles.x * tileSize) - Vector3.left * (tileSize);
        rotation = new Vector3(0, 0, 0);
        PlaceDoor(spawnPos, false, doorL, rotation);
        }
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
    void GenerateRoomTiles(int numNeighbors, int type, bool removeWalls)
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
        else if (removeWalls)
        {
            bool upperWall = true;
            bool bottomWall = true;
            bool leftWall = true;
            bool rightWall = true;

            if (gridPos.x == 0)
            {
                width -= 1;
                rightWall = false;

                // Dont remove pillar between rooms
                GenerateTile(width, 0);
            }
            else 
            { 
                xStart++;
                leftWall = false;

                // Dont remove pillar between rooms
                GenerateTile(width - 1, height - 1);
            }

            if (gridPos.y == 0)
            {
                yStart++;
                upperWall = false;

                // Dont remove pillar between rooms
                GenerateTile(width, height - 1);
            }
            else 
            { 
                height -= 1;
                bottomWall = false;

                // Dont remove pillar between rooms
                GenerateTile(0, height);
            }

            PlaceWallsOnDoors(upperWall, bottomWall, leftWall, rightWall);
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
        //find the color to match the pixel
        foreach (ColorToGameObject mapping in mappings)
        {
            if (mapping.color.Equals(pixelColor))
            {
                // Debug.Log(mapping.color + ", " + pixelColor);   
                Vector3 spawnPos = positionFromTileGrid(x, y);
                GameObject obj = Instantiate(mapping.prefab, spawnPos, Quaternion.identity);
                obj.transform.parent = this.transform;

                // For delivery points (purple) and pickup points (yellow), set the gameObject as deactivated
                if (obj.CompareTag("PickupPoint"))
                {
                    obj.transform.rotation = Quaternion.Euler(-90, 0, 0); // Pickup point face upwards
                    obj.SetActive(false);
                }
                if (obj.CompareTag("DeliveryPoint"))
                {
                    obj.transform.position += new Vector3(0, 0, -8); // Delivery point center offset
                    obj.SetActive(false);
                }
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
