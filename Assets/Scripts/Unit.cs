using UnityEngine;

public class Unit : MonoBehaviour
{
    public bool isAlly = true;
    public int moveRange = 3;
    public int remainingMoveRange = 3; // 이번 턴에 남은 이동력
    public Vector2Int currentPos;
    public bool isSelected = false;
    public bool hasActedThisRound = false; // 이번 라운드에 행동했는지 여부
    
    private BattleManager battleManager;
    private RoundManager roundManager;
    private Renderer unitRenderer;
    private Color originalColor;
    
    void Awake()
    {
        unitRenderer = GetComponent<Renderer>();
        if (unitRenderer == null)
        {
            // Renderer가 없으면 자식에서 찾기
            unitRenderer = GetComponentInChildren<Renderer>();
        }
        
        // Collider가 없으면 추가
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, 1f);
            Debug.Log("유닛에 BoxCollider 추가됨");
        }
    }
    
    void Start()
    {
        // BattleManager 찾기
        battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogError("BattleManager를 찾을 수 없음!");
        }
        
        // RoundManager 찾기
        roundManager = FindObjectOfType<RoundManager>();
        if (roundManager == null)
        {
            Debug.LogError("RoundManager를 찾을 수 없음!");
        }
        
        // 초기 색상 설정
        SetUnitColor();
    }
    
    void SetUnitColor()
    {
        if (unitRenderer != null)
        {
            if (isAlly)
            {
                originalColor = Color.blue;
                unitRenderer.material.color = Color.blue;
            }
            else
            {
                originalColor = Color.red;
                unitRenderer.material.color = Color.red;
            }
        }
    }
    
    void Update()
    {
        // 선택 상태에 따른 시각적 표시
        if (unitRenderer != null)
        {
            if (isSelected)
            {
                // 선택된 유닛은 테두리 효과 (색상 밝게)
                unitRenderer.material.color = Color.Lerp(originalColor, Color.white, 0.5f);
            }
            else
            {
                unitRenderer.material.color = originalColor;
            }
        }
    }
    
    void OnMouseDown()
    {
        Debug.Log($"유닛 클릭됨: isAlly={isAlly}, battleManager={battleManager != null}");
        
        if (battleManager != null && isAlly)
        {
            Debug.Log("SelectUnit 호출 중...");
            battleManager.SelectUnit(this);
            Debug.Log($"유닛 선택됨: {currentPos}");
        }
        else if (battleManager == null)
        {
            Debug.LogError("BattleManager가 null임!");
        }
        else if (!isAlly)
        {
            Debug.Log("적군 유닛은 선택할 수 없음");
        }
    }
} 