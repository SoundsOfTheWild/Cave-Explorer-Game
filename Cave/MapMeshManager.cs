using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MapMeshManager : MonoBehaviour {

	[SerializeField]
    private GameObject background;
	[SerializeField]
    private GameObject cellHolder;
	[SerializeField]
    private GameObject cell;
    [SerializeField]
    private GameObject outerWallsPrefab;
    [SerializeField]
    private int borderSize = 20;

	[SerializeField] private GameObject walls;
    MeshCollider wallCollider;

    [SerializeField]
    private bool gizmos;

	[SerializeField]
    private Sprite[] edgeSprites;
	[SerializeField]
    private Sprite[] fullSprites;
	[Range (0, 100)]
	[SerializeField]
    private int rockDensity = 20;

    [SerializeField]
    private SquareGrid squareGrid;
	GameObject[,] cells;
    GameObject outerWalls;

	List<Vector3> globVertices;

	Dictionary<int, List<Triangle>> triangleDictionary= new Dictionary<int, List<Triangle>>();
	List<List<int>> outlines = new List<List<int>>();
	HashSet<int> checkedVertices = new HashSet<int> ();

	public void GenerateMesh(int[,] map, float squareSize, int gridWidth, int gridHeight) {

        //Resets previously generated mesh
		DestroyWallMesh ();

		squareGrid = new SquareGrid(map, squareSize);
		cells = new GameObject[squareGrid.squares.GetLength(0), squareGrid.squares.GetLength(1)];

        //List of vertices for all triangles
		globVertices = new List<Vector3>();

		int mapWidth = squareGrid.squares.GetLength (0);
		int mapHeight = squareGrid.squares.GetLength (1);

        //Offset for centre of squares
		Vector3 offset = new Vector3 ((float)mapWidth / 2f - 0.5f, (float)mapHeight / 2f - 0.5f, 0f);

		for (int x = 0; x < mapWidth; x ++) {
			for (int y = 0; y < mapHeight; y ++) {

                //List of vertices for individual cell
				List<Vector3> locVertices = new List<Vector3> ();
				List<int> locTriangles = new List<int>();

                //Generates the triangle according to marching squares for this cell
				cells [x, y] = Instantiate (cell, new Vector3((float)x, (float)y, 0f) - offset, Quaternion.identity, cellHolder.transform);

				SpriteRenderer sprite = cells [x, y].GetComponentInChildren <SpriteRenderer> ();
				sprite.sprite = TriangulateSquare(squareGrid.squares[x,y], locTriangles, locVertices);

                //Creates mesh using local vertices and indices
				Mesh mesh = new Mesh ();
				mesh.vertices = locVertices.ToArray();
				mesh.triangles = locTriangles.ToArray();
				mesh.RecalculateNormals();

                //Sets the new cell to have the calculated mesh
				cells [x, y].GetComponent<MeshFilter> ().mesh = mesh;

			}
		}

        //Outer square boundary surrounding the whole map
        GenerateBoundaryWalls(squareSize, (float)gridWidth, (float)gridHeight);

        //Scales the background object to cover the entire map
        background.transform.localScale = new Vector3(mapWidth, mapHeight, 1f) * squareSize;

        //Creates edge colliders of map
        Generate2DColliders();

        //Camera should be orthographics for 2D
        //Camera.main.orthographic = true;
	}

    void GenerateBoundaryWalls(float squareSize, float mapWidth, float mapHeight) {
        mapWidth *= squareSize;
        mapHeight *= squareSize;

        outerWalls = Instantiate(outerWallsPrefab, cellHolder.transform.parent.transform);
		outerWalls.transform.localScale = new Vector3 (mapWidth, mapHeight, 1f) * 2;
    }

    void Generate2DColliders()
    {
        //Remove existing colliders
        DestroyColliders();

        //Gets outlines of regions
        CalculateMeshOutlines();

        //Generates collider for each outline
        foreach(List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++)
            {
                edgePoints[i] = globVertices[outline[i]];
            }
            edgeCollider.points = edgePoints;
        }
    }

	Sprite TriangulateSquare(Square square, List<int> triangles, List<Vector3> vertices) {
        //Switch statement encapsulating all possible marching square configurations
		switch (square.configuration) {
		case 0:
			return edgeSprites [0];

			// 1 points:
		case 1:
			MeshFromPoints(triangles, vertices, square.centreLeft, square.centreBottom, square.bottomLeft);
			return edgeSprites [1];
		case 2:
			MeshFromPoints(triangles, vertices, square.bottomRight, square.centreBottom, square.centreRight);
			return edgeSprites [2];
		case 4:
			MeshFromPoints(triangles, vertices, square.topRight, square.centreRight, square.centreTop);
			return edgeSprites [4];
		case 8:
			MeshFromPoints(triangles, vertices, square.topLeft, square.centreTop, square.centreLeft);
			return edgeSprites [8];

			// 2 points:
		case 3:
			MeshFromPoints(triangles, vertices, square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
			return edgeSprites [3];
		case 6:
			MeshFromPoints(triangles, vertices, square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
			return edgeSprites [6];
		case 9:
			MeshFromPoints(triangles, vertices, square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
			return edgeSprites [9];
		case 12:
			MeshFromPoints(triangles, vertices, square.topLeft, square.topRight, square.centreRight, square.centreLeft);
			return edgeSprites [12];
		case 5:
			MeshFromPoints(triangles, vertices, square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
			return edgeSprites [5];
		case 10:
			MeshFromPoints(triangles, vertices, square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
			return edgeSprites [10];

			// 3 point:
		case 7:
			MeshFromPoints(triangles, vertices, square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
			return edgeSprites [7];
		case 11:
			MeshFromPoints(triangles, vertices, square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
			return edgeSprites [11];
		case 13:
			MeshFromPoints(triangles, vertices, square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
			return edgeSprites [13];
		case 14:
			MeshFromPoints (triangles, vertices, square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
			return edgeSprites [14];

			// 4 point:
		case 15:
			MeshFromPoints (triangles, vertices, square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
            //Optimisation: a square with all surounding points on cannot be an edge. Checked vertices are skipped in looking for edges
			checkedVertices.Add (square.topLeft.globIndex);
			checkedVertices.Add (square.topRight.globIndex);
			checkedVertices.Add (square.bottomRight.globIndex);
			checkedVertices.Add (square.bottomLeft.globIndex);

			if (Random.Range (0, 101) < rockDensity) {
				return fullSprites [Random.Range (1, 5)];
			} else {
					return fullSprites[0];
			}
		}

		return edgeSprites [square.configuration];

	}

	void MeshFromPoints(List<int> triangles, List<Vector3> vertices, params Node[] points) {
		
		for (int i = 0; i < points.Length; i ++) {
            //Adds new vertices to global list
			if (points[i].globIndex == -1) {
				points[i].globIndex = globVertices.Count;
				globVertices.Add(points[i].position);
			}
            //Adds vertices to local list
			vertices.Add (points [i].position);
		}
		if (points.Length >= 3)
			CreateTriangle(0,1,2, triangles, points);
		if (points.Length >= 4)
			CreateTriangle(0,2,3, triangles, points);
		if (points.Length >= 5) 
			CreateTriangle(0,3,4, triangles, points);
		if (points.Length >= 6)
			CreateTriangle(0,4,5, triangles, points);

	}
		

	void CreateTriangle(int indexA, int indexB, int indexC, List<int> triangles, params Node[] points) {
		triangles.Add(indexA);
		triangles.Add(indexB);
		triangles.Add(indexC);

        //Creates the traingle
		Triangle triangle = new Triangle (points[indexA].globIndex, points[indexB].globIndex, points[indexC].globIndex);
		AddTriangleToDictionary (triangle.vertexIndexA, triangle);
		AddTriangleToDictionary (triangle.vertexIndexB, triangle);
		AddTriangleToDictionary (triangle.vertexIndexC, triangle);
	}

	void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle) {
        //Dictionary of all triangles with key of their globl index
		if (triangleDictionary.ContainsKey (vertexIndexKey)) {
			triangleDictionary [vertexIndexKey].Add (triangle);
		} else {
			List<Triangle> triangleList = new List<Triangle>();
			triangleList.Add(triangle);
			triangleDictionary.Add(vertexIndexKey, triangleList);
		}
	}

	void CalculateMeshOutlines() {

		for (int vertexIndex = 0; vertexIndex < globVertices.Count; vertexIndex ++) {
			if (!checkedVertices.Contains(vertexIndex)) {
				int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
				if (newOutlineVertex != -1) {
					checkedVertices.Add(vertexIndex);

					List<int> newOutline = new List<int>();
					newOutline.Add(vertexIndex);
					outlines.Add(newOutline);
					FollowOutline(newOutlineVertex, outlines.Count-1);
					outlines[outlines.Count-1].Add(vertexIndex);
				}
			}
		}
	}

	void FollowOutline(int vertexIndex, int outlineIndex) {
		outlines [outlineIndex].Add (vertexIndex);
        //Don't want to calculate any edge more than once, so each vertex along it is added to checked vertices
		checkedVertices.Add (vertexIndex);
		int nextVertexIndex = GetConnectedOutlineVertex (vertexIndex);

		if (nextVertexIndex != -1) {
			FollowOutline(nextVertexIndex, outlineIndex);
		}
	}

	int GetConnectedOutlineVertex(int vertexIndex) {
        //Find all triangles containing this vertex
		List<Triangle> trianglesContainingVertex = triangleDictionary [vertexIndex];

        //Check if any other vertex in any of those triangles is also on the edge
		for (int i = 0; i < trianglesContainingVertex.Count; i ++) {
			Triangle triangle = trianglesContainingVertex[i];

			for (int j = 0; j < 3; j ++) {
				int vertexB = triangle[j];
				if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB)) {
					if (IsOutlineEdge(vertexIndex, vertexB)) {
						return vertexB;
					}
				}
			}
		}

        //Only reach this is the vertex was already checked, i.e. the starting point of the current edge
		return -1;
	}

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
                //No two vertices can share more than one traingle and still be an edge
                if (sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

    void OnDrawGizmos() {
        //Draws gizmos representing the nodes (black for walls, white for space)
		if (squareGrid != null && gizmos) {
			for (int x = 0; x < squareGrid.squares.GetLength(0); x ++) {
				for (int y = 0; y < squareGrid.squares.GetLength(1); y ++) {
					Gizmos.color = (squareGrid.squares[x,y].topLeft.active)?Color.black:Color.white;
					Gizmos.DrawCube(squareGrid.squares[x,y].topLeft.position, Vector3.one * .4f);
					Gizmos.color = (squareGrid.squares[x,y].topRight.active)?Color.black:Color.white;
					Gizmos.DrawCube(squareGrid.squares[x,y].topRight.position, Vector3.one * .4f);
					Gizmos.color = (squareGrid.squares[x,y].bottomRight.active)?Color.black:Color.white;
					Gizmos.DrawCube(squareGrid.squares[x,y].bottomRight.position, Vector3.one * .4f);
					Gizmos.color = (squareGrid.squares[x,y].bottomLeft.active)?Color.black:Color.white;
					Gizmos.DrawCube(squareGrid.squares[x,y].bottomLeft.position, Vector3.one * .4f);
					Gizmos.color = Color.grey;
					Gizmos.DrawCube(squareGrid.squares[x,y].centreTop.position, Vector3.one * .15f);
					Gizmos.DrawCube(squareGrid.squares[x,y].centreRight.position, Vector3.one * .15f);
					Gizmos.DrawCube(squareGrid.squares[x,y].centreBottom.position, Vector3.one * .15f);
					Gizmos.DrawCube(squareGrid.squares[x,y].centreLeft.position, Vector3.one * .15f);
				}
			}
		}
	}

	public void DestroyAllChildren() {
		while (cellHolder.transform.childCount > 0) {
			foreach (Transform child in cellHolder.transform) {
				DestroyImmediate (child.gameObject);
			}
		}
	}

	public void DestroyWallMesh() {
		triangleDictionary.Clear ();
		outlines.Clear ();
		checkedVertices.Clear ();
		walls.GetComponent<MeshFilter>().mesh = new Mesh ();
        if (wallCollider != null)
        {
            wallCollider.sharedMesh = new Mesh();
        }
        DestroyImmediate(outerWalls);
	}

    public void DestroyColliders()
    {
        //Removes existing colliders (remove this and start map before final builds)
        while (gameObject.GetComponents<EdgeCollider2D>().Length > 0)
        {
            DestroyImmediate(gameObject.GetComponents<EdgeCollider2D>()[0]);
        }
    }

	struct Triangle {
		public int vertexIndexA;
		public int vertexIndexB;
		public int vertexIndexC;
		int[] vertices;

		public Triangle (int a, int b, int c) {
			vertexIndexA = a;
			vertexIndexB = b;
			vertexIndexC = c;

			vertices = new int[3];
			vertices[0] = a;
			vertices[1] = b;
			vertices[2] = c;
		}

		public int this[int i] {
			get {
				return vertices[i];
			}
		}


		public bool Contains(int vertexIndex) {
			return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
		}
	}

	public class SquareGrid {
		public Square[,] squares;

		public SquareGrid(int[,] map, float squareSize) {
			int nodeCountX = map.GetLength(0);
			int nodeCountY = map.GetLength(1);
			float mapWidth = nodeCountX * squareSize;
			float mapHeight = nodeCountY * squareSize;

			ControlNode[,] controlNodes = new ControlNode[nodeCountX,nodeCountY];
			squares = new Square[nodeCountX -1,nodeCountY -1];

			for (int x = 0; x < nodeCountX; x ++) {
				for (int y = 0; y < nodeCountY; y ++) {
					Vector2 pos = new Vector2(-mapWidth/2 + x * squareSize + squareSize/2, -mapHeight/2 + y * squareSize + squareSize/2);
					controlNodes[x,y] = new ControlNode(pos,map[x,y] == 1, squareSize);
					if(x>0 && y > 0) {
						squares[x - 1,y - 1] = new Square(controlNodes[x - 1,y], controlNodes[x,y], controlNodes[x,y - 1], controlNodes[x - 1,y - 1]);
					}
				}
			}

			for (int x = 0; x < nodeCountX-1; x ++) {
				for (int y = 0; y < nodeCountY-1; y ++) {
				}
			}

		}
	}

	public class Square {

		public ControlNode topLeft, topRight, bottomRight, bottomLeft;
		public Node centreTop, centreRight, centreBottom, centreLeft;
		public Vector3 position;
		public int configuration;

		public Square (ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
			topLeft = _topLeft;
			topRight = _topRight;
			bottomRight = _bottomRight;
			bottomLeft = _bottomLeft;

			centreTop = topLeft.right;
			centreRight = bottomRight.above;
			centreBottom = bottomLeft.right;
			centreLeft = bottomLeft.above;

			if (topLeft.active)
				configuration += 8;
			if (topRight.active)
				configuration += 4;
			if (bottomRight.active)
				configuration += 2;
			if (bottomLeft.active)
				configuration += 1;
		}
	}

	public class Node {
		public Vector3 position;
		public int globIndex = -1;

		public Node(Vector3 _pos) {
			position = _pos;
		}
	}

	public class ControlNode : Node {

		public bool active;
		public Node above, right;

		public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos) {
			active = _active;
			above = new Node(position + Vector3.up * squareSize/2f);
			right = new Node(position + Vector3.right * squareSize/2f);
		}

	}
}