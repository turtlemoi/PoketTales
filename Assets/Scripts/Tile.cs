using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool isWalkable = true;
    public Unit unitOnTile = null;
    
    private BattleManager battleManager;
    
    void Awake()
    {
        // 타일에 시각적 요소가 없으면 자동으로 생성
        if (GetComponent<Renderer>() == null && GetComponentInChildren<Renderer>() == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
            
            // 기본 색상 설정
            Renderer renderer = cube.GetComponent<Renderer>();
            renderer.material.color = Color.gray;
            Debug.Log($"타일 at {gridPosition}에 시각적 요소 생성됨");
        }

        // Collider가 없으면 추가
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, 1f);
        }
    }
    
    void Start()
    {
        battleManager = FindObjectOfType<BattleManager>();
        
        // 타일에 기본 색상 설정 (회색)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = GetComponentInChildren<Renderer>();
        }
        
        if (renderer != null)
        {
            renderer.material.color = Color.gray;
        }
    }

    void OnMouseEnter()
    {
        if (battleManager != null && battleManager.selectedUnit != null)
        {
            // 이동 가능한 타일이면 경로 표시
            if (battleManager.moveableTiles.Contains(this))
            {
                battleManager.ShowPath(gridPosition);
            }
        }
    }
    
    void OnMouseExit()
    {
        if (battleManager != null)
        {
            battleManager.ClearPathTiles();
        }
    }

    void OnMouseDown()
    {
        if (battleManager != null && battleManager.selectedUnit != null)
        {
            // 이동 가능한 타일이면 이동
            if (battleManager.moveableTiles.Contains(this))
            {
                battleManager.MoveUnit(gridPosition);
                Debug.Log($"유닛이 위치로 이동: {gridPosition}");
            }
        }
        
        Debug.Log($"타일 클릭됨: 위치 {gridPosition}");
        Debug.Log($"이동 가능: {isWalkable}");
        Debug.Log($"타일 위 유닛: {(unitOnTile != null ? unitOnTile.name : "없음")}");
    }
}
