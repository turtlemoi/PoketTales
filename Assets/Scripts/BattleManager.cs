using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject allyUnitPrefab;
    public GameObject enemyUnitPrefab;
    
    [Header("Map Reference")]
    public MapManager mapManager;
    
    [Header("Round Manager Reference")]
    public RoundManager roundManager;
    
    [Header("Unit Lists")]
    public List<Unit> allyUnits = new List<Unit>();
    public List<Unit> enemyUnits = new List<Unit>();
    
    [Header("Selection")]
    public Unit selectedUnit = null;
    public List<Tile> moveableTiles = new List<Tile>();
    public List<Tile> pathTiles = new List<Tile>();
    
    // 문자열 캐싱 (TLS Allocator 문제 해결)
    private string unitSelectionLogCache;
    private string unitMoveLogCache;
    
    void Start()
    {
        // 맵 매니저 참조 가져오기
        if (mapManager == null)
            mapManager = FindObjectOfType<MapManager>();
            
        // 라운드 매니저 참조 가져오기
        if (roundManager == null)
            roundManager = FindObjectOfType<RoundManager>();
            
        // 맵이 완전히 생성된 후 유닛 배치
        StartCoroutine(InitializeAfterMapReady());
    }
    
    System.Collections.IEnumerator InitializeAfterMapReady()
    {
        // 맵 매니저가 완전히 초기화될 때까지 대기
        while (mapManager == null || mapManager.tiles == null)
        {
            yield return new WaitForEndOfFrame();
        }
        
        // 초기 유닛 배치
        PlaceInitialUnits();
        
        // 라운드 매니저에 유닛 배치 완료 알림
        if (roundManager != null)
        {
            roundManager.OnUnitsPlaced();
        }
    }
    
    void PlaceInitialUnits()
    {
        Debug.Log("초기 유닛 배치 시작");
        
        // 프리팹 확인
        if (allyUnitPrefab == null)
        {
            Debug.LogError("아군 유닛 프리팹이 할당되지 않았습니다!");
            return;
        }
        
        if (enemyUnitPrefab == null)
        {
            Debug.LogError("적군 유닛 프리팹이 할당되지 않았습니다!");
            return;
        }
        
        // 아군 유닛 배치 (왼쪽)
        PlaceUnit(allyUnitPrefab, new Vector2Int(1, 1), true);
        PlaceUnit(allyUnitPrefab, new Vector2Int(1, 2), true);
        PlaceUnit(allyUnitPrefab, new Vector2Int(2, 1), true);
        
        // 적군 유닛 배치 (오른쪽)
        PlaceUnit(enemyUnitPrefab, new Vector2Int(8, 8), false);
        PlaceUnit(enemyUnitPrefab, new Vector2Int(8, 9), false);
        PlaceUnit(enemyUnitPrefab, new Vector2Int(9, 8), false);
        
        Debug.Log($"유닛 배치 완료: 아군 {allyUnits.Count}개, 적군 {enemyUnits.Count}개");
    }
    
    void PlaceUnit(GameObject unitPrefab, Vector2Int position, bool isAlly)
    {
        // 프리팹 확인
        if (unitPrefab == null)
        {
            Debug.LogError($"{(isAlly ? "아군" : "적군")} 유닛 프리팹이 null입니다!");
            return;
        }
        
        // 위치 확인
        if (!IsValidPosition(position))
        {
            Debug.LogError($"유효하지 않은 위치: {position}");
            return;
        }
        
        if (mapManager.tiles[position.x, position.y].unitOnTile != null)
        {
            Debug.LogWarning($"위치 {position}에 이미 유닛이 있습니다.");
            return; // 이미 유닛이 있는 경우
        }
            
        Vector3 worldPos = new Vector3(position.x, 0.5f, position.y);
        GameObject unitObj = Instantiate(unitPrefab, worldPos, Quaternion.identity);
        
        if (unitObj == null)
        {
            Debug.LogError($"유닛 오브젝트 생성 실패: {position}");
            return;
        }
        
        Unit unit = unitObj.GetComponent<Unit>();
        
        if (unit == null)
        {
            Debug.LogError($"Unit 컴포넌트를 찾을 수 없습니다: {unitObj.name}");
            Destroy(unitObj);
            return;
        }
        
        unit.currentPos = position;
        unit.isAlly = isAlly;
        
        // 타일에 유닛 정보 설정
        mapManager.tiles[position.x, position.y].unitOnTile = unit;
        
        // 리스트에 추가
        if (isAlly)
            allyUnits.Add(unit);
        else
            enemyUnits.Add(unit);
    }
    
    public void SelectUnit(Unit unit)
    {
        // 이전 선택 해제
        if (selectedUnit != null)
        {
            selectedUnit.isSelected = false;
            ClearMoveableTiles();
            ClearPathTiles();
        }
        
        // 새 유닛 선택
        if (unit != null && roundManager != null)
        {
            if (roundManager.isAllyTurn && unit.isAlly)
            {
                // 아군 턴에 아군 유닛 선택 (플레이어가 직접 선택)
                selectedUnit = unit;
                unit.isSelected = true;
                
                // 현재 활성 유닛이 있고, 선택하려는 유닛이 현재 활성 유닛과 다른 경우
                // 이동 가능한 타일을 표시하지 않음
                if (roundManager.currentActiveUnit != null && unit != roundManager.currentActiveUnit)
                {
                    Debug.Log($"다른 아군이 이미 행동 중입니다. {roundManager.currentActiveUnit.name}의 행동을 완료해주세요.");
                    return;
                }
                
                // 현재 활성 유닛이 없으면 이 유닛을 활성 유닛으로 설정
                if (roundManager.currentActiveUnit == null)
                {
                    roundManager.currentActiveUnit = unit;
                    Debug.Log($"아군 유닛이 활성화됨: {unit.name} at {unit.currentPos}, 남은 이동력: {unit.remainingMoveRange}");
                }
                
                ShowMoveableTiles(unit);
                Debug.Log($"아군 유닛 선택됨: {unit.name} at {unit.currentPos}, 남은 이동력: {unit.remainingMoveRange}");
            }
            else if (!roundManager.isAllyTurn && !unit.isAlly)
            {
                // 적군 턴에 적군 유닛 선택 (AI용)
                selectedUnit = unit;
                unit.isSelected = true;
                ShowMoveableTiles(unit);
            }
        }
        else
        {
            selectedUnit = null;
        }
    }
    
    void ShowMoveableTiles(Unit unit)
    {
        moveableTiles.Clear();
        
        // BFS로 이동 가능한 타일 찾기
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(unit.currentPos);
        visited.Add(unit.currentPos);
        
        int moveRange = unit.remainingMoveRange; // 남은 이동력 사용
        
        while (queue.Count > 0 && moveRange >= 0)
        {
            int levelSize = queue.Count;
            
            for (int i = 0; i < levelSize; i++)
            {
                Vector2Int current = queue.Dequeue();
                
                // 현재 위치가 이동 범위 내에 있으면 표시
                if (Vector2Int.Distance(unit.currentPos, current) <= unit.remainingMoveRange)
                {
                    Tile tile = mapManager.tiles[current.x, current.y];
                    if (tile.isWalkable && tile.unitOnTile == null)
                    {
                        moveableTiles.Add(tile);
                        HighlightTile(tile, Color.cyan);
                    }
                }
                
                // 인접한 타일들 확인
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int next = current + dir;
                    
                    if (IsValidPosition(next) && !visited.Contains(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
            moveRange--;
        }
    }
    
    public void ShowPath(Vector2Int targetPos)
    {
        if (selectedUnit == null) return;
        
        ClearPathTiles();
        
        // A* 알고리즘으로 경로 찾기
        List<Vector2Int> path = FindPath(selectedUnit.currentPos, targetPos);
        
        if (path != null && path.Count > 0)
        {
            foreach (Vector2Int pos in path)
            {
                Tile tile = mapManager.tiles[pos.x, pos.y];
                pathTiles.Add(tile);
                HighlightTile(tile, Color.yellow);
            }
        }
    }
    
    public void ClearPathTiles()
    {
        foreach (Tile tile in pathTiles)
        {
            // 이동 가능한 타일이면 청색으로, 아니면 회색으로
            if (moveableTiles.Contains(tile))
                HighlightTile(tile, Color.cyan);
            else
                HighlightTile(tile, Color.gray);
        }
        pathTiles.Clear();
    }
    
    public void MoveUnit(Vector2Int targetPos)
    {
        if (selectedUnit == null) return;
        
        // 이동 거리 계산
        int moveDistance = Mathf.RoundToInt(Vector2Int.Distance(selectedUnit.currentPos, targetPos));
        
        // 이동력이 부족한지 확인
        if (moveDistance > selectedUnit.remainingMoveRange)
        {
            Debug.LogWarning($"이동력이 부족합니다. 필요: {moveDistance}, 남은 이동력: {selectedUnit.remainingMoveRange}");
            return;
        }
        
        // 현재 선택된 유닛 저장 (SelectUnit(null) 호출 후 참조가 사라질 수 있음)
        Unit movedUnit = selectedUnit;
        bool isAllyUnit = movedUnit.isAlly;
        
        // 기존 위치에서 유닛 제거
        mapManager.tiles[movedUnit.currentPos.x, movedUnit.currentPos.y].unitOnTile = null;
        
        // 새 위치로 이동
        movedUnit.currentPos = targetPos;
        movedUnit.transform.position = new Vector3(targetPos.x, 0.5f, targetPos.y);
        
        // 이동력 소모
        movedUnit.remainingMoveRange -= moveDistance;
        
        // 새 위치에 유닛 정보 설정
        mapManager.tiles[targetPos.x, targetPos.y].unitOnTile = movedUnit;
        
        Debug.Log($"유닛 이동: {movedUnit.name} 이동력 {moveDistance} 소모, 남은 이동력: {movedUnit.remainingMoveRange}");
        
        // 선택 해제
        SelectUnit(null);
        
        // 이동 완료 후 처리
        if (roundManager != null)
        {
            if (isAllyUnit)
            {
                // 아군 유닛 이동: 턴이 자동으로 종료되지 않음
                // 플레이어가 턴 종료 버튼을 누르거나 Space 키를 눌러야 턴이 종료됨
                Debug.Log("아군 유닛 이동 완료. 추가 행동이 가능합니다.");
            }
            else
            {
                // 적군 유닛 이동: 턴이 자동으로 종료됨
                roundManager.OnUnitMoved();
            }
        }
    }
    
    List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // 간단한 BFS 경로 찾기
        Queue<List<Vector2Int>> queue = new Queue<List<Vector2Int>>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(new List<Vector2Int> { start });
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            List<Vector2Int> currentPath = queue.Dequeue();
            Vector2Int current = currentPath[currentPath.Count - 1];
            
            if (current == end)
                return currentPath;
            
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                
                if (IsValidPosition(next) && !visited.Contains(next))
                {
                    Tile tile = mapManager.tiles[next.x, next.y];
                    if (tile.isWalkable && tile.unitOnTile == null)
                    {
                        visited.Add(next);
                        List<Vector2Int> newPath = new List<Vector2Int>(currentPath);
                        newPath.Add(next);
                        queue.Enqueue(newPath);
                    }
                }
            }
        }
        
        return null;
    }
    
    public bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapManager.width && 
               pos.y >= 0 && pos.y < mapManager.height;
    }
    
    void HighlightTile(Tile tile, Color color)
    {
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = tile.GetComponentInChildren<Renderer>();
        }
        
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        else
        {
            Debug.LogError($"타일 at {tile.gridPosition}에 Renderer 없음");
        }
    }
    
    void ClearMoveableTiles()
    {
        foreach (Tile tile in moveableTiles)
        {
            HighlightTile(tile, Color.gray);
        }
        moveableTiles.Clear();
    }
} 