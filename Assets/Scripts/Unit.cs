using UnityEngine;

public class Unit : MonoBehaviour
{
    public bool isAlly = true;
    public int moveRange = 3;
    public Vector2Int currentPos;
    public bool isSelected = false;
    
    private BattleManager battleManager;
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
            Debug.Log("Added BoxCollider to Unit");
        }
    }
    
    void Start()
    {
        // BattleManager 찾기
        battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogError("BattleManager not found!");
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
        Debug.Log($"Unit clicked: isAlly={isAlly}, battleManager={battleManager != null}");
        
        if (battleManager != null && isAlly)
        {
            Debug.Log("Calling SelectUnit...");
            battleManager.SelectUnit(this);
            Debug.Log($"Selected unit at position: {currentPos}");
        }
        else if (battleManager == null)
        {
            Debug.LogError("BattleManager is null!");
        }
        else if (!isAlly)
        {
            Debug.Log("Cannot select enemy unit");
        }
    }
} 