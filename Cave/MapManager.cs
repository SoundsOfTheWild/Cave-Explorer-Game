using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(MapMeshManager))]
public class MapManager : MonoBehaviour {

    [Header("Size Properties")]
	[Range(1, 1000)]
	[SerializeField]
    private int gridWidth = 100;
	[Range(1,1000)]
	[SerializeField]
    private int gridHeight = 100;
	[SerializeField]
    private float gridSize = 1f;
	[Range(1, 20)]
	[SerializeField]
    private int borderSize = 5;
    [SerializeField]
    private int wallThresholdSize = 50;
    [SerializeField]
    private int roomThresholdSize = 50;
    [SerializeField]
    private int passageWidth = 2;
    [SerializeField]
    private int startRoomRadX = 8;
    [SerializeField]
    private int startRoomRadY = 4;
    [SerializeField]
    private int startRoomWallRad = 6;
    [SerializeField]
    private int sideRoomRadX = 6;
    [SerializeField]
    private int sideRoomRadY = 3;

    [Header("Algorithm Properties")]
	[Range(0,100)]
	[SerializeField]
    private int fillPercent = 50;
	[Range(1,10)]
	[SerializeField]
    private int smoothIterations = 4;
	[Range(1,6)]
	[SerializeField]
    private int neighboursToSmoothToWall = 4;

	[Header("Seed Properties")]
	[SerializeField]
    private int seed = 0;
    [SerializeField]
    private bool useRandomSeed = false;

    //Stores data of spaces and walls of map
	[SerializeField]
    private int[,] mapData;

    private List<Room> rooms;
    //Stores wether or bot floodfill has checked each point
    private int[,] mapFlags;
    private System.Random pseudoRandom;

    private MapMeshManager meshManager;

    private Coord centre, leftCentre, rightCentre;
    private int leftRoomHeight;
    private int leftRoomDistance;
    private int rightRoomHeight;
    private int rightRoomDistance;

    private Room finalRoom;
   // private List<Edge> allEdges;


	//TODO Remove this when publishing, change to pick a random map (and remove the prebuilt map from the scene in the editor)
    void Awake()
    {
        //GETS MESH WHEN GAME IS RUNNING
        if (Application.isPlaying) {
            meshManager = GetComponent<MapMeshManager>();
            meshManager.DestroyAllChildren();
            meshManager.DestroyWallMesh();
            GenerateMap();
        }
    }
    
    void Update()
    {
        //LEVEL RELOAD TO TEST GENERATION (i.e. game is running)
        if(Input.GetKeyDown(KeyCode.Return))
        {
            SceneManager.LoadScene("DevScene");
        }
    }

    //All other methods called from here
    public void GenerateMap() {
        //INITIALISATION
        if (meshManager == null)
        {
            //GETS MESH IF GAME NOT RUNNING (i.e. Awake() wasn't called)
            meshManager = GetComponent<MapMeshManager>();
        }
        mapData = new int[gridWidth, gridHeight];
        mapFlags = new int[gridWidth, gridHeight];
        rooms = new List<Room>();


        //GENERATE RANDOM PARAMETERS FOR SIDE ROOMS
        leftRoomHeight = UnityEngine.Random.Range(0, 4) * ((int)UnityEngine.Random.Range(0, 2) * 2 - 1);
        rightRoomHeight = UnityEngine.Random.Range(0, 4) * ((int)UnityEngine.Random.Range(0, 2) * 2 - 1);
        leftRoomDistance = UnityEngine.Random.Range(2, 6);
        rightRoomDistance = UnityEngine.Random.Range(2, 6);

        //GENERATION

        //Random 0s (space) and 1s (walls) set according to fill percent
        GenerateNoise ();

        //Noise transformed to areas of similar type and made not to be jagged
		Smooth (smoothIterations);

        //Central rooms set in map data, also added to rooms list and map flags set to 1
        GenerateMainRoom(true);

        /*Areas smaller than thresholds removed (i.e. tiny rooms and small floating walls),
         * then regions that are spaces are added to a list (minus the very central rooms,
         * then connected until every room is connected to one of the side rooms */
        ProcessRegions();

        //Smooths again incase passages are not smooth
        Smooth(smoothIterations);

        //Side central rooms connected to middle
        CreatePassage(rooms[0], rooms[1], centre, leftCentre, -1);
        CreatePassage(rooms[0], rooms[2], centre, rightCentre, -1);

        /*
        //GETTING EDGES
        mapFlags = new int[gridWidth, gridHeight];
        allEdges = new List<Edge>();
        finalRoom = new Room(GetRegions(0)[0], mapData, false, false);
        foreach (Coord edge in finalRoom.edgeTiles) {
            allEdges.Add(new Edge(edge.tileX, edge.tileY));
        }
        */

        //ADDS BORDERS
        int[,] borderedMapData = new int[gridWidth + borderSize * 2, gridHeight + borderSize * 2];
		for (int x = 0; x < borderedMapData.GetLength(0); x++) {
			for (int y = 0; y < borderedMapData.GetLength(1); y++) {
				if (x >= borderSize && x < gridWidth + borderSize && y >= borderSize && y < gridHeight + borderSize) {
					borderedMapData[x,y] = mapData[x-borderSize, y - borderSize];
				}
				else {
					borderedMapData[x,y] = 1;
				}
			}
		}

        //GENERATES MESH
		meshManager.GenerateMesh(borderedMapData, gridSize, gridWidth, gridHeight);
    }

    //Forces three rooms to exist in the centre of the map
    void GenerateMainRoom(bool first = false)
    {
        centre = new Coord(gridWidth / 2, gridHeight / 2);
        leftCentre = new Coord(gridWidth / 2  - startRoomRadX - leftRoomDistance, gridHeight / 2 + leftRoomHeight);
        rightCentre = new Coord(gridWidth / 2 + startRoomRadX + rightRoomDistance, gridHeight / 2 + rightRoomHeight);

        //Walls surrounding the main rooms are not regenerated incase they block off a previouslt connected passage
        if (first) {
            DrawOval(centre, startRoomRadX + startRoomWallRad, startRoomRadY + startRoomWallRad, 1);
            DrawOval(leftCentre, sideRoomRadX + 2, sideRoomRadY + 2, 1);
            DrawOval(rightCentre, sideRoomRadX + 2, sideRoomRadY + 2, 1);
        }


        DrawOval(centre, startRoomRadX, startRoomRadY, 0, false, true, first);
        DrawOval(leftCentre, sideRoomRadX, sideRoomRadY, 0, true, false, first);
        DrawOval(rightCentre, sideRoomRadX, sideRoomRadY, 0, true, false, first);

  
    }

    //Sets 2D array to 0s(space) and 1s(walls) according to map fill percent
    void GenerateNoise() {

		if (useRandomSeed) {
            pseudoRandom = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
		} else {
			pseudoRandom = new System.Random (seed);
		}
		
		for (int x = 0; x < gridWidth; x++) {
			for (int y = 0; y < gridHeight; y++) {
				if(x == 0 || x == gridWidth - 1 || y == 0 || y == gridHeight - 1 ) {
                    //Edges initially forced to be walls
					mapData [x, y] = 1;
				} else {
                    //Fill percent of the map is set to walls, the rest to space
					mapData [x, y] = (pseudoRandom.Next (0, 100) < fillPercent) ? 1 : 0;
				}
			}
		}
	}

    //Dense areas of space set entirely to space, and likewise with walls
	void Smooth(int iterations) {
        /*If a point is surrounded by more walls than space, then it is set to space, and vice cersa.
         * If they are equal the point is unchanged*/
		for (int i = 0; i < iterations; i++) {

			for (int x = 0; x < gridWidth; x++) {
				for (int y = 0; y < gridHeight; y++) {
					int neighbourWallTiles = GetSurroundingWallCount(x,y);

					if (neighbourWallTiles > neighboursToSmoothToWall)
						mapData [x, y] = 1;
					else if (neighbourWallTiles < neighboursToSmoothToWall)
						mapData [x, y] = 0;
				}
			}

		}

	}

    //Returns number or walls in the 8 surround points of the input
	int GetSurroundingWallCount(int gridX, int gridY) {
        //Retrieves the total number of walls of the 8 point around the input point
		int wallCount = 0;
		for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX ++) {
			for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY ++) {
				if (IsInMapRange(neighbourX,neighbourY)) {
					if (neighbourX != gridX || neighbourY != gridY) {
						wallCount += mapData[neighbourX,neighbourY];
					}
				}
				else {
					wallCount ++;
				}
			}
		}

		return wallCount;
	}

    //Checks that a given point is within the map
	bool IsInMapRange(int x, int y) {
        //Returns true if x and y are valid indices for map data
		return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
	}

    //Calls the floofill and connection methods
    void ProcessRegions()
    {
        //Deletes wall regions less with fewer than wall threshold total points
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Coord tile in wallRegion)
                {
                    mapData[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        //Deletes space regions less with fewer than wall threshold total points
        List<List<Coord>> roomRegions = GetRegions(0);

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    mapData[tile.tileX, tile.tileY] = 1;
                }
            }
        }

        //Resets mapflags for re-retrieving the rooms
        mapFlags = new int[gridWidth, gridHeight];
        //Regenerated the main rooms in case they were smaller than the threshold
        GenerateMainRoom();
        roomRegions = GetRegions(0);

        foreach(List<Coord> room in roomRegions) {
            rooms.Add(new Room(room, mapData, false, false));
        }

        List<Room> roomsWithoutSubmain = new List<Room>();
        roomsWithoutSubmain = rooms;
        roomsWithoutSubmain.RemoveAt(0);

        //Connects rooms but ignores the central room
        ConnectClosestRooms(roomsWithoutSubmain, true);
    }

    //First connects clossest rooms, then makes sure all rooms are connected to one of the two side centre rooms
    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false) {

        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAccessibilityFromMainRoom) {
            foreach (Room room in allRooms) {
                if (room.isAccessible) {
                    //List of rooms accessible from the main rooms
                    roomListB.Add(room);
                } else {
                    //List of rooms isolated from the main rooms
                    roomListA.Add(room);
                }
            }
        } else {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        //connects a room from the first list to the clossest room in the second
        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA) {
        //On first pass (not forcing accessibility) rooms already connected to any other are subsequently ignored
            if (!forceAccessibilityFromMainRoom) {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0) {
                    continue;
                }
            }

            foreach (Room roomB in roomListB) {
                if (roomA == roomB || roomA.IsConnected(roomB)) {
                    continue;
                }

                //Iterates through all rooms in each list and finds the shortest distance between all pairs
                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++) {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++) {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound) {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
            //On first pass every room connects to its clossest counterpart
            if (possibleConnectionFound && !forceAccessibilityFromMainRoom) {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        //On subsequent passes only the shortest connection that makes more rooms accessible from the main room are made
        if (possibleConnectionFound && forceAccessibilityFromMainRoom) {
            //Third and beyond passes called from here
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (!forceAccessibilityFromMainRoom) {
            //Second pass called here
            ConnectClosestRooms(allRooms, true);
        }
    }

    //Draws 0s along a line, creating a tunnel connection
    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB, int sizeModifier = 0)
    {
        //Rooms know they are connected (and then check if they have become accessible from the main rooms
        Room.ConnectRooms(roomA, roomB);

        List<Coord> line = GetLine(tileA, tileB);

        foreach(Coord c in line)
        {
            //A circle at each point in the line creates a tunnel
            DrawCircle(c, passageWidth + sizeModifier);
        }
    }

    //Sets points within a radius of a centre to space
    void DrawCircle(Coord c, int rad)
    {
        for (int x = -rad; x <= rad; x ++)
        {
            for (int y = -rad; y <= rad; y++)
            {
                if(x*x + y*y <= rad*rad)
                {
                    int newX = c.tileX + x;
                    int newY = c.tileY + y;

                    if (IsInMapRange(newX, newY))
                    {
                        //Sets all points within the radius of the centre (and within the map) to space
                        mapData[newX, newY] = 0;
                    }
                } 
            }
        }
    }

    //Sets points withing the radii (major and minor axes) of a centre point to the desired type
    void DrawOval(Coord c, int radX, int radY, int type, bool main = false, bool submain = false, bool first = true)
    {
        List<Coord> room = new List<Coord>();

        for (int x = -radX; x <= radX; x++)
        {
            for (int y = -radY; y <= radY; y++)
            {
                if ((float)(x * x)/(float)(radX * radX) + (float)(y * y)/(float)(radY * radY) <= 1f)
                {
                    int newX = c.tileX + x;
                    int newY = c.tileY + y;

                    if (IsInMapRange(newX, newY))
                    {
                        //Sets all points within the radis of the centre (and within the map) to the specified
                        mapData[newX, newY] = type;

                        //When called from GenerateMainRooms (the first time), this adds those rooms to the list of rooms and sets their map flags
                        if ((main || submain) && first) {
                            room.Add(new Coord(newX, newY));
                            mapFlags[newX, newY] = 1;

                        }
                    }
                }
            }
        }

        if ((main||submain) && first) {
            rooms.Add(new Room(room, mapData, main, submain));
        }
    }

    //Returns the points that lie clossest to the line between two given poitns
    List<Coord> GetLine(Coord start, Coord end)
    {
        //See Sebastian Lague's YouTube tutorial for the logic of this method. Returns a list of points which lie on the line between the inputs
        List<Coord> line = new List<Coord>();
        bool inverted = false;

        int x = start.tileX;
        int y = start.tileY;

        int dx = end.tileX - start.tileX;
        int dy = end.tileY - start.tileY;

        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));
            if(inverted)
            {
                y += step;
            } else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if(gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                } else
                {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }

        return line;
    }

    List<List<Coord>> GetRegions (int tileType) {
        //Floodfills each region of the same type, returns the regions as a list
        List<List<Coord>> regions = new List<List<Coord>>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (mapFlags[x, y] == 0 && mapData[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    //Floodfills from a start point
	List<Coord> GetRegionTiles (int startX, int startY) {
        List<Coord> tiles = new List<Coord>();
        //Resets map flags for the multiple uses of this method
        int[,] mapFlags = new int[gridWidth, gridHeight];
        int tileType = mapData[startX, startY];

        //Floodfill queue
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && mapData[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    //struct representing a point at x and y in map data
	struct Coord {
		public int tileX;
		public int tileY;

		public Coord(int x , int y) {
			tileX = x;
			tileY = y;
		}
	}

    //struct to be implemented for edges to spawn the mineable material on
 /*
    struct Edge {
        public int tileX;
        public int tileY;
        public int distance;

        public Edge(int x, int y) {
            tileX = x;
            tileY = y;
            distance = (int)(Mathf.Pow(tileX, 2) + Mathf.Pow(tileY, 2));
        }
    }
*/

        //class containing information and functionality for rooms
    class Room
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessible;
        public bool isMainRoom;
        public bool isSubmainRoom;

        public Room()
        {
        }

        public Room(List<Coord> roomTiles, int[,] map, bool main, bool submain)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();
            isMainRoom = main;
            isSubmainRoom = submain;
            isAccessible = main || submain;

            edgeTiles = new List<Coord>();
            foreach (Coord tile in tiles)
            {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (map[x, y] == 1)
                            {
                                //If the above, below, left or right point is a wall, then this point is defined as an edge tile
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }

        public void SetAccessibleFromMainRoom()
        {
            if (!isAccessible)
            {
                isAccessible = true;
                foreach (Room connectedRoom in connectedRooms)
                {
                    connectedRoom.SetAccessibleFromMainRoom();
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isAccessible)
            {
                roomB.SetAccessibleFromMainRoom();
            }
            else if (roomB.isAccessible)
            {
                roomA.SetAccessibleFromMainRoom();
            }
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }
    }

}
