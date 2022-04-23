using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Line
{
    public Vector3Int lineNode1, lineNode2;
    public bool isHorizontal;

    public Line(Vector3Int lineNode1, Vector3Int lineNode2)
    {
        this.lineNode1 = lineNode1;
        this.lineNode2 = lineNode2;
    }
}

public class MapGenerator : MonoBehaviour
{
    private List<TreeNode> treeList = new List<TreeNode>();
    private List<TreeNode> roomList = new List<TreeNode>();

    private List<Line> lineList = new List<Line>();

    [SerializeField]
    private Vector2Int bottomLeft, topRight;

    [SerializeField]
    private int roomMinSize;
    [SerializeField]
    private int maxDepth;
    private int minDepth = 0;

    private int[,] map = new int[1000, 1000];

    [SerializeField]
    private Tile wall;
    [SerializeField]
    private Tile road;
    [SerializeField]
    private Tile InRoom;
    [SerializeField]
    private Tile Line;
    [SerializeField]
    private Tilemap wallTilemap;
    [SerializeField]
    private Tilemap roadTilemap;
    [SerializeField]
    private Tilemap InRoomTilemap;
    [SerializeField]
    private Tilemap LineTilemap;

    private void Start()
    {
        //0 = 비어있음, 1 = 방 테두리, 2 = 방사이 길, 3 = 방내부 블럭
        GenerateMap();
    }

    void GenerateMap()
    {
        for (int y = 0; y < topRight.y; y++)
        {
            for (int x = 0; x < topRight.x; x++)
            {
                map[y, x] = 0;
            }
        }

        treeList.Clear();
        roomList.Clear();
        lineList.Clear();

        TreeNode root = root = new TreeNode(bottomLeft, topRight);
        treeList.Add(root);
        ToMakeTree(ref root, minDepth);
        ToMakeRoom();
        ConnectRoom();
        ExtendLine();
        BuildWall();
        CreateTileMap();
    }

    void ToMakeTree(ref TreeNode node, int depth)   // 재귀로 방을 만들어주는 함수
    {
        node.depth = depth;
        if (depth >= maxDepth)
            return;
        int divideRatio = Random.Range(30, 71);
        depth++;
        node.SetDirection();
        if (node.DivideNode(divideRatio, roomMinSize))
        {
            ToMakeTree(ref node.leftNode, depth);
            ToMakeTree(ref node.rightNode, depth);
            treeList.Add(node.leftNode);
            treeList.Add(node.rightNode);
        }
    }

    void ToMakeRoom()   // 방에 값을 설정해주는 함수 
    {
        for (int x = 0; x < treeList.Count; x++)
        {
            treeList[x].CreateRoom();

            if (!treeList[x].isDivided)
            {
                continue;
            }
            for (int ry = treeList[x].roomBL.y; ry <= treeList[x].roomTR.y; ry++)
            {
                for (int rx = treeList[x].roomBL.x; rx <= treeList[x].roomTR.x; rx++)
                {
                    if (rx == treeList[x].roomBL.x || rx == treeList[x].roomTR.x || ry == treeList[x].roomBL.y ||
                        ry == treeList[x].roomTR.y) // 테두리는 벽이기떄문에 검사 해서 1로 설정
                    {
                        map[ry, rx] = 1;
                    }
                    else    // 테두리가 아니면 3으로 설정함
                    {
                        map[ry, rx] = 3;
                    }
                }
            }
            roomList.Add(treeList[x]);
        }
    }

    void ConnectRoom()  // 방끼리 연결해주는 함수
    {
        for (int x = 0; x < treeList.Count; x++)
        {
            for (int y = 0; y < treeList.Count; y++)
            {
                if (treeList[x] != treeList[y] && treeList[x].parentNode == treeList[y].parentNode)
                {
                    if (treeList[x].parentNode.GetDirection())
                    {
                        int temp = (treeList[x].parentNode.leftNode.topRight.y + treeList[x].parentNode.leftNode.bottomLeft.y) / 2;
                        Line line = new Line(new Vector3Int(treeList[x].parentNode.leftNode.topRight.x - 2, temp, 0), new Vector3Int(treeList[y].parentNode.rightNode.bottomLeft.x + 2, temp, 0));
                        lineList.Add(line);
                        MarkLineOnMap(line);
                    }
                    else
                    {
                        int temp = (treeList[x].parentNode.leftNode.topRight.x + treeList[x].parentNode.leftNode.bottomLeft.y) / 2;
                        Line line = new Line(new Vector3Int(temp, treeList[x].parentNode.leftNode.topRight.y - 2, 0), new Vector3Int(temp, treeList[y].parentNode.rightNode.bottomLeft.y + 2, 0));
                        lineList.Add(line);
                        MarkLineOnMap(line);
                    }
                }
            }
        }
    }

    void MarkLineOnMap(Line line)   // 방 사이에 길을 놔주는 함수
    {
        if (line.lineNode1.x == line.lineNode2.x)
        {
            for (int y = line.lineNode1.y; y <= line.lineNode2.y; y++)
            {
                map[y, line.lineNode1.x] = 2;
            }
        }
        else
        {
            for (int x = line.lineNode1.x; x <= line.lineNode2.x; x++)
            {
                map[line.lineNode1.y, x] = 2;
            }
        }
    }

    void ExtendLine()   // 방과 방끼리 길을 이어주는 함수
    {
        for (int x = 0; x < lineList.Count; x++)
        {
            if (lineList[x].isHorizontal)
            {
                while (true)
                {
                    int lx = lineList[x].lineNode1.x;
                    int ly = lineList[x].lineNode1.y;
                    if (map[ly, lx - 1] == 0 || map[ly, lx - 1] == 1)
                    {
                        if (map[ly + 1, lx] == 2 || map[ly - 1, lx] == 2 || map[ly + 1, lx] == 3 || map[ly - 1, lx] == 3)
                        {
                            break;
                        }
                        map[ly, lx - 1] = 2;
                        lineList[x].lineNode1.x = lx - 1;
                    }
                    else break;
                }

                while (true)
                {
                    int lx = lineList[x].lineNode2.x;
                    int ly = lineList[x].lineNode2.y;
                    if (map[ly, lx + 1] == 0 || map[ly, lx + 1] == 1)
                    {
                        if (map[ly + 1, lx] == 2 || map[ly - 1, lx] == 2 || map[ly + 1, lx] == 3 || map[ly - 1, lx] == 3)
                        {
                            break;
                        }
                        map[ly, lx + 1] = 2;
                        lineList[x].lineNode2.x = lx + 1;
                    }
                    else break;
                }
            }
            if (!lineList[x].isHorizontal)
            {
                while (true)
                {
                    int lx = lineList[x].lineNode2.x;
                    int ly = lineList[x].lineNode2.y;
                    if (map[ly + 1, lx] == 0 || map[ly + 1, lx] == 1)
                    {
                        if (map[ly, lx + 1] == 2 || map[ly, lx - 1] == 2 || map[ly, lx + 1] == 3 || map[ly, lx - 1] == 3)
                        {
                            break;
                        }
                        map[ly + 1, lx] = 2;
                        lineList[x].lineNode2.y = ly + 1;
                    }
                    else break;
                }
                while (true)
                {
                    int lx = lineList[x].lineNode1.x;
                    int ly = lineList[x].lineNode1.y;
                    if (map[ly - 1, lx] == 0 || map[ly - 1, lx] == 1)
                    {
                        if (map[ly, lx + 1] == 2 || map[ly, lx - 1] == 2 || map[ly, lx + 1] == 3 || map[ly, lx - 1] == 3)
                        {
                            break;
                        }
                        map[ly - 1, lx] = 2;
                        lineList[x].lineNode1.y = ly - 1;
                    }
                    else break;
                }
            }
        }
    }

    void BuildWall()    // 테두리 벽을 생성해주는 함수
    {
        for (int y = 0; y < topRight.y; y++)
        {
            for (int x = 0; x <= topRight.x; x++)
            {
                if (map[y, x] != 2)
                    continue;
                for (int xx = -1; xx <= 1; xx++)
                {
                    for (int yy = -1; yy <= 1; yy++)
                    {
                        if (map[y + yy, x + xx] != 0)
                            continue;
                        map[y + yy, x + xx] = 1;
                    }
                }
            }
        }
    }

    void CreateTileMap()    // 타일맵을 생성해주는 함수 
    {
        for (int y = 0; y < topRight.y; y++)
        {
            for (int x = 0; x < topRight.x; x++)
            {
                if (map[y, x] == 0)
                {
                    roadTilemap.SetTile(new Vector3Int(x, y, 0), road);
                }
                else if (map[y, x] == 1)
                {
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), wall);
                }
                else if (map[y, x] == 2)
                {
                    LineTilemap.SetTile(new Vector3Int(x, y, 0), Line);
                }
                else if (map[y, x] == 3)
                {
                    InRoomTilemap.SetTile(new Vector3Int(x, y, 0), InRoom);
                }
            }
        }
    }
}
