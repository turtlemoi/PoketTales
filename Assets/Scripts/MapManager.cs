using UnityEngine;

public class MapManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 10;
    public int height = 10;
    public Tile[,] tiles;

    void Awake()
    {
        GenerateMap();
    }

    void OnValidate()
    {
        // 에디터에서 값이 변경될 때 맵 재생성 (선택사항)
        if (Application.isPlaying == false)
        {
            // 에디터에서 미리보기용 (선택사항)
        }
    }

    void GenerateMap()
    {
        // 기존 타일들 제거
        if (tiles != null)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    if (tiles[x, y] != null)
                    {
                        DestroyImmediate(tiles[x, y].gameObject);
                    }
                }
            }
        }

        tiles = new Tile[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 맵을 (0,0,0) 근처에 생성하도록 조정
                GameObject tileObj = Instantiate(tilePrefab, new Vector3(x, 0, y), Quaternion.identity);
                Tile tile = tileObj.GetComponent<Tile>();
                tile.gridPosition = new Vector2Int(x, y);
                tiles[x, y] = tile;
            }
        }
        
        Debug.Log($"맵 생성 완료: {width}x{height} 크기");
    }
}
