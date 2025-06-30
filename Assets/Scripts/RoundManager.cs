using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RoundManager : MonoBehaviour
{
    [Header("Round Settings")]
    public int currentRound = 1;
    public bool isAllyTurn = true; // true: 아군 턴, false: 적군 턴
    
    [Header("Unit Management")]
    public List<Unit> allUnits = new List<Unit>(); // 모든 유닛
    public List<Unit> availableAllyUnits = new List<Unit>(); // 이번 라운드에 아직 행동하지 않은 아군 유닛들
    public List<Unit> availableEnemyUnits = new List<Unit>(); // 이번 라운드에 아직 행동하지 않은 적군 유닛들
    public Unit currentActiveUnit = null; // 현재 행동 중인 유닛
    public bool waitingForPlayerInput = true; // 플레이어 입력 대기 중인지
    public bool hasUnits = false; // 유닛이 있는지 확인하는 플래그
    
    [Header("UI References")]
    public Text roundText;
    public Text phaseText;
    public Button endActionButton;
    
    [Header("References")]
    public BattleManager battleManager;
    
    // 문자열 캐싱 (TLS Allocator 문제 해결)
    private string roundTextCache;
    private string phaseTextCache;
    
    // 행동 페이즈 열거형
    public enum ActionPhase
    {
        Start,      // 행동 시작
        Movement,   // 이동
        Action,     // 행동 (공격 등)
        End         // 행동 종료
    }
    
    public ActionPhase currentPhase = ActionPhase.Start;
    
    void Start()
    {
        // BattleManager 참조 가져오기
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();
            
        // UI 초기화
        InitializeUI();
        
        // BattleManager가 유닛을 배치한 후에 라운드 시작
        // 첫 라운드는 OnUnitsPlaced()에서 시작됨
    }
    
    void InitializeUI()
    {
        // 행동 종료 버튼 이벤트 연결
        if (endActionButton != null)
        {
            endActionButton.onClick.AddListener(EndCurrentAction);
        }
        
        UpdateUI();
    }
    
    void UpdateUI()
    {
        // 라운드 정보 업데이트 (문자열 캐싱 사용)
        if (roundText != null)
        {
            if (roundTextCache == null || !roundTextCache.Contains(currentRound.ToString()))
            {
                roundTextCache = $"Round {currentRound}";
                roundText.text = roundTextCache;
            }
        }
        
        // 페이즈 정보 업데이트 (문자열 캐싱 사용)
        if (phaseText != null)
        {
            string currentUnitInfo = "";
            if (currentActiveUnit != null)
            {
                currentUnitInfo = $" - {(currentActiveUnit.isAlly ? "아군" : "적군")} 유닛 at {currentActiveUnit.currentPos}";
            }
            
            string newPhaseText = $"Phase: {currentPhase}{currentUnitInfo}";
            if (phaseTextCache != newPhaseText)
            {
                phaseTextCache = newPhaseText;
                phaseText.text = phaseTextCache;
            }
        }
        
        // 행동 종료 버튼 활성화/비활성화
        if (endActionButton != null)
        {
            endActionButton.interactable = isAllyTurn && waitingForPlayerInput; // 아군 턴에만 활성화
        }
    }
    
    public void StartNewRound()
    {
        Debug.Log($"라운드 {currentRound} 시작");
        
        // 행동 시작 페이즈
        currentPhase = ActionPhase.Start;
        
        // 선택된 유닛 해제
        if (battleManager != null)
        {
            battleManager.SelectUnit(null);
        }
        
        // 모든 유닛 목록 설정
        SetupAllUnits();
        
        // 이번 라운드에 행동 가능한 유닛들 설정
        SetupAvailableUnits();
        
        // 아군 턴으로 시작
        isAllyTurn = true;
        waitingForPlayerInput = true;
        
        // 라운드 시작 효과 적용
        ApplyRoundStartEffects();
        
        // UI 업데이트
        UpdateUI();
        
        // 첫 번째 턴 시작
        StartNextTurn();
    }
    
    void StartNextTurn()
    {
        // 현재 턴에 행동할 유닛 선택
        if (isAllyTurn)
        {
            // 아군 턴: 아직 행동하지 않은 아군 유닛 중 하나 선택
            if (availableAllyUnits.Count > 0)
            {
                currentActiveUnit = null; // 아군 턴 시작 시 활성 유닛 초기화
                waitingForPlayerInput = true;
                Debug.Log($"아군 턴 시작: 플레이어가 행동할 아군을 선택하세요.");
                
                // 아군 턴에서는 플레이어가 유닛을 선택할 수 있도록 자동 선택하지 않음
                // 플레이어가 직접 클릭해서 선택해야 함
            }
            else
            {
                // 아군이 모두 행동 완료, 적군 턴으로
                isAllyTurn = false;
                StartNextTurn();
                return;
            }
        }
        else
        {
            // 적군 턴: 아직 행동하지 않은 적군 유닛 중 하나 선택
            if (availableEnemyUnits.Count > 0)
            {
                currentActiveUnit = availableEnemyUnits[0];
                waitingForPlayerInput = false;
                Debug.Log($"적군 턴 시작: {currentActiveUnit.name} at {currentActiveUnit.currentPos}");
                
                // 현재 활성 유닛을 선택 상태로 만들기
                if (battleManager != null)
                {
                    battleManager.SelectUnit(currentActiveUnit);
                }
                
                ExecuteAIUnitAction(currentActiveUnit);
            }
            else
            {
                // 적군이 모두 행동 완료, 아군 턴으로
                isAllyTurn = true;
                StartNextTurn();
                return;
            }
        }
        
        UpdateUI();
    }
    
    void SetupAllUnits()
    {
        allUnits.Clear();
        
        // BattleManager가 null인지 확인
        if (battleManager == null)
        {
            Debug.LogError("BattleManager가 null입니다!");
            hasUnits = false;
            return;
        }
        
        // 아군 유닛들 추가
        if (battleManager.allyUnits != null)
        {
            foreach (Unit unit in battleManager.allyUnits)
            {
                if (unit != null)
                {
                    allUnits.Add(unit);
                }
            }
        }
        
        // 적군 유닛들 추가
        if (battleManager.enemyUnits != null)
        {
            foreach (Unit unit in battleManager.enemyUnits)
            {
                if (unit != null)
                {
                    allUnits.Add(unit);
                }
            }
        }
        
        // 유닛이 없으면 경고
        if (allUnits.Count == 0)
        {
            Debug.LogWarning("유닛이 없습니다! BattleManager에서 유닛이 제대로 생성되었는지 확인하세요.");
            hasUnits = false;
            return;
        }
        
        hasUnits = true;
        
        // 나중에 속도 스탯에 따라 정렬할 예정
        // allUnits.Sort((a, b) => b.speed.CompareTo(a.speed));
        
        // 임시로 랜덤 순서로 섞기
        for (int i = 0; i < allUnits.Count; i++)
        {
            Unit temp = allUnits[i];
            int randomIndex = Random.Range(i, allUnits.Count);
            allUnits[i] = allUnits[randomIndex];
            allUnits[randomIndex] = temp;
        }
    }
    
    void SetupAvailableUnits()
    {
        availableAllyUnits.Clear();
        availableEnemyUnits.Clear();
        
        // 모든 유닛을 이번 라운드에 행동 가능한 유닛으로 추가
        foreach (Unit unit in allUnits)
        {
            if (unit != null)
            {
                // 라운드 시작 시 유닛 상태 초기화
                unit.hasActedThisRound = false;
                unit.remainingMoveRange = unit.moveRange;
                
                if (unit.isAlly)
                {
                    availableAllyUnits.Add(unit);
                }
                else
                {
                    availableEnemyUnits.Add(unit);
                }
            }
        }
        
        // 나중에 속도 스탯에 따라 정렬할 예정
        // availableAllyUnits.Sort((a, b) => b.speed.CompareTo(a.speed));
        // availableEnemyUnits.Sort((a, b) => b.speed.CompareTo(a.speed));
        
        // 임시로 랜덤 순서로 섞기
        for (int i = 0; i < availableAllyUnits.Count; i++)
        {
            Unit temp = availableAllyUnits[i];
            int randomIndex = Random.Range(i, availableAllyUnits.Count);
            availableAllyUnits[i] = availableAllyUnits[randomIndex];
            availableAllyUnits[randomIndex] = temp;
        }
        
        for (int i = 0; i < availableEnemyUnits.Count; i++)
        {
            Unit temp = availableEnemyUnits[i];
            int randomIndex = Random.Range(i, availableEnemyUnits.Count);
            availableEnemyUnits[i] = availableEnemyUnits[randomIndex];
            availableEnemyUnits[randomIndex] = temp;
        }
    }
    
    void ProcessNextUnit()
    {
        // 모든 유닛이 행동 완료했는지 확인
        if (availableAllyUnits.Count == 0 && availableEnemyUnits.Count == 0)
        {
            Debug.Log($"모든 유닛이 행동 완료, 라운드 {currentRound} 종료");
            EndRound();
            return;
        }
        
        // 턴 전환
        if (isAllyTurn)
        {
            // 아군 턴이 종료되면 적군 턴으로
            isAllyTurn = false;
            Debug.Log("아군 턴 종료, 적군 턴으로 전환");
        }
        else
        {
            // 적군 턴이 종료되면 아군 턴으로
            isAllyTurn = true;
            Debug.Log("적군 턴 종료, 아군 턴으로 전환");
        }
        
        // 다음 턴 시작
        StartNextTurn();
    }
    
    public void OnUnitMoved()
    {
        // 아군 턴에서는 유닛 이동 후에도 턴이 자동으로 종료되지 않음
        // 플레이어가 턴 종료 버튼을 누르거나 Space 키를 눌러야 턴이 종료됨
        if (isAllyTurn)
        {
            Debug.Log("아군 유닛 이동 완료. 턴 종료 버튼을 누르거나 Space 키를 눌러 턴을 종료하세요.");
            return;
        }
        
        // 적군 턴에서는 유닛 이동 후 자동으로 턴 종료
        // 현재 유닛을 해당 리스트에서 제거 (이번 라운드에 행동 완료)
        if (currentActiveUnit != null)
        {
            currentActiveUnit.hasActedThisRound = true;
            
            if (currentActiveUnit.isAlly)
            {
                availableAllyUnits.Remove(currentActiveUnit);
            }
            else
            {
                availableEnemyUnits.Remove(currentActiveUnit);
            }
            Debug.Log($"유닛 행동 완료: {currentActiveUnit.name} at {currentActiveUnit.currentPos}");
        }
        
        // 현재 유닛 초기화
        currentActiveUnit = null;
        
        // BattleManager에서 선택 해제
        if (battleManager != null)
        {
            battleManager.SelectUnit(null);
        }
        
        // 다음 턴으로
        ProcessNextUnit();
    }
    
    public void EndCurrentAction()
    {
        // 아군 턴에서만 턴 종료 가능
        if (!isAllyTurn)
        {
            Debug.Log("적군 턴에서는 턴을 수동으로 종료할 수 없습니다.");
            return;
        }
        
        Debug.Log("플레이어가 현재 행동을 종료함");
        
        // 현재 활성 유닛을 해당 리스트에서 제거 (이번 라운드에 행동 완료)
        if (currentActiveUnit != null)
        {
            currentActiveUnit.hasActedThisRound = true;
            
            if (currentActiveUnit.isAlly)
            {
                availableAllyUnits.Remove(currentActiveUnit);
            }
            else
            {
                availableEnemyUnits.Remove(currentActiveUnit);
            }
            Debug.Log($"유닛 행동 완료: {currentActiveUnit.name} at {currentActiveUnit.currentPos}");
        }
        
        // 현재 유닛 초기화
        currentActiveUnit = null;
        
        // BattleManager에서 선택 해제
        if (battleManager != null)
        {
            battleManager.SelectUnit(null);
        }
        
        // 다음 턴으로
        ProcessNextUnit();
    }
    
    void ApplyRoundStartEffects()
    {
        // 이번 라운드에 행동할 유닛들의 라운드 시작 효과
        foreach (Unit unit in availableAllyUnits)
        {
            if (unit != null)
            {
                // 여기에 라운드 시작 효과 추가 (예: 체력 회복, 상태이상 해제 등)
                // Debug.Log($"유닛 at {unit.currentPos} 라운드 시작 효과 적용"); // 로그 제거로 성능 개선
            }
        }
        
        foreach (Unit unit in availableEnemyUnits)
        {
            if (unit != null)
            {
                // 여기에 라운드 시작 효과 추가 (예: 체력 회복, 상태이상 해제 등)
                // Debug.Log($"유닛 at {unit.currentPos} 라운드 시작 효과 적용"); // 로그 제거로 성능 개선
            }
        }
    }
    
    public void EndRound()
    {
        Debug.Log($"라운드 {currentRound} 종료");
        
        // 유닛이 없으면 게임 일시정지
        if (!hasUnits)
        {
            Debug.LogError("유닛이 없어서 게임을 일시정지합니다. BattleManager에서 유닛을 생성해주세요.");
            return;
        }
        
        // 라운드 수 증가
        currentRound++;
        
        // 새 라운드 시작
        StartNewRound();
    }
    
    void ExecuteAIUnitAction(Unit unit)
    {
        Debug.Log($"AI 유닛 행동 실행: {unit.currentPos}");
        
        // 간단한 AI: 랜덤 이동
        Vector2Int[] possibleMoves = GetPossibleMoves(unit);
        
        if (possibleMoves.Length > 0)
        {
            Vector2Int randomMove = possibleMoves[Random.Range(0, possibleMoves.Length)];
            Debug.Log($"AI 유닛 이동: {unit.currentPos} → {randomMove}");
            
            // AI 유닛을 선택 상태로 만들고 이동
            battleManager.SelectUnit(unit);
            battleManager.MoveUnit(randomMove);
            
            // AI 유닛은 이동 후 바로 다음 유닛으로 넘어감
            // MoveUnit에서 OnUnitMoved가 호출되므로 여기서는 추가 호출하지 않음
        }
        else
        {
            Debug.Log("AI 유닛 이동 가능한 위치 없음");
            // 이동할 수 없어도 다음 유닛으로
            Invoke("OnUnitMoved", 0.5f);
        }
    }
    
    Vector2Int[] GetPossibleMoves(Unit unit)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        
        // 단순히 주변 4방향만 확인
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = unit.currentPos + dir;
            
            if (battleManager.IsValidPosition(newPos))
            {
                Tile tile = battleManager.mapManager.tiles[newPos.x, newPos.y];
                if (tile.isWalkable && tile.unitOnTile == null)
                {
                    moves.Add(newPos);
                }
            }
        }
        
        return moves.ToArray();
    }
    
    // 키보드 단축키
    void Update()
    {
        // Space 키로 현재 행동 종료 (아군 턴에만, 플레이어 입력 대기 중일 때만)
        if (Input.GetKeyDown(KeyCode.Space) && isAllyTurn && waitingForPlayerInput)
        {
            EndCurrentAction();
        }
    }
    
    // BattleManager에서 유닛 배치 완료 후 호출
    public void OnUnitsPlaced()
    {
        StartNewRound();
    }
} 