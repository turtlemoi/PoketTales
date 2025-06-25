using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject allyUnitPrefab;
    public GameObject enemyUnitPrefab;
    
    [Header("Map Reference")]
    public MapManager mapManager;
    
    [Header("Unit Lists")]
    public List<Unit> allyUnits = new List<Unit>();
    public List<Unit> enemyUnits = new List<Unit>();
    
    [Header("Selection")]
    public Unit selectedUnit = null;
    public List<Tile> moveableTiles = new List<Tile>();
    public List<Tile> pathTiles = new List<Tile>();
    
    void Start()
    {
        // 맵 매니저 참조 가져오기
        if (mapManager == null)
            mapManager = FindObjectOfType<MapManager>();
            
        // 초기 유닛 배치
        PlaceInitialUnits();
    }
    
    void PlaceInitialUnits()
    {
        // 아군 유닛 배치 (왼쪽)
        PlaceUnit(allyUnitPrefab, new Vector2Int(1, 1), true);
        PlaceUnit(allyUnitPrefab, new Vector2Int(1, 2), true);
        PlaceUnit(allyUnitPrefab, new Vector2Int(2, 1), true);
        
        // 적군 유닛 배치 (오른쪽)
        PlaceUnit(enemyUnitPrefab, new Vector2Int(8, 8), false);
        PlaceUnit(enemyUnitPrefab, new Vector2Int(8, 9), false);
        PlaceUnit(enemyUnitPrefab, new Vector2Int(9, 8), false);
    }
    
    void PlaceUnit(GameObject unitPrefab, Vector2Int position, bool isAlly)
    {
        if (mapManager.tiles[position.x, position.y].unitOnTile != null)
            return; // 이미 유닛이 있는 경우
            
        Vector3 worldPos = new Vector3(position.x, 0.5f, position.y);
        GameObject unitObj = Instantiate(unitPrefab, worldPos, Quaternion.identity);
        Unit unit = unitObj.GetComponent<Unit>();
        
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
        Debug.Log($"SelectUnit called with unit: {(unit != null ? unit.name : "null")}");
        
        // 이전 선택 해제
        if (selectedUnit != null)
        {
            Debug.Log("Clearing previous selection");
            selectedUnit.isSelected = false;
            ClearMoveableTiles();
            ClearPathTiles();
        }
        
        // 새 유닛 선택
        selectedUnit = unit;
        if (unit != null)
        {
            Debug.Log($"Selecting new unit: {unit.name} at position {unit.currentPos}");
            unit.isSelected = true;
            ShowMoveableTiles(unit);
        }
        else
        {
            Debug.Log("No unit selected");
        }
    }
    
    void ShowMoveableTiles(Unit unit)
    {
        Debug.Log($"ShowMoveableTiles called for unit at position: {unit.currentPos}");
        moveableTiles.Clear();
        
        // BFS로 이동 가능한 타일 찾기
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(unit.currentPos);
        visited.Add(unit.currentPos);
        
        int moveRange = unit.moveRange;
        Debug.Log($"Unit move range: {moveRange}");
        
        while (queue.Count > 0 && moveRange >= 0)
        {
            int levelSize = queue.Count;
            Debug.Log($"Processing level {unit.moveRange - moveRange}, queue size: {levelSize}");
            
            for (int i = 0; i < levelSize; i++)
            {
                Vector2Int current = queue.Dequeue();
                
                // 현재 위치가 이동 범위 내에 있으면 표시
                if (Vector2Int.Distance(unit.currentPos, current) <= unit.moveRange)
                {
                    Tile tile = mapManager.tiles[current.x, current.y];
                    if (tile.isWalkable && tile.unitOnTile == null)
                    {
                        moveableTiles.Add(tile);
                        HighlightTile(tile, Color.cyan);
                        Debug.Log($"Added moveable tile at: {current}");
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
        
        Debug.Log($"Total moveable tiles found: {moveableTiles.Count}");
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
        
        // 기존 위치에서 유닛 제거
        mapManager.tiles[selectedUnit.currentPos.x, selectedUnit.currentPos.y].unitOnTile = null;
        
        // 새 위치로 이동
        selectedUnit.currentPos = targetPos;
        selectedUnit.transform.position = new Vector3(targetPos.x, 0.5f, targetPos.y);
        
        // 새 위치에 유닛 정보 설정
        mapManager.tiles[targetPos.x, targetPos.y].unitOnTile = selectedUnit;
        
        // 선택 해제
        SelectUnit(null);
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
    
    bool IsValidPosition(Vector2Int pos)
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
            Debug.Log($"Highlighted tile at {tile.gridPosition} with color: {color}");
        }
        else
        {
            Debug.LogError($"No Renderer found on tile at {tile.gridPosition}");
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