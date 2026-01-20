using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 드래그 선택 영역의 시각적 렌더링을 담당
/// 독립 모드: 자체적으로 마우스 입력 처리
/// 외부 제어 모드: SelectionManager 등 외부에서 IsDragging, StartMousePos 설정
/// </summary>
public class DragRenderer : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private bool useStandaloneMode = true; // 독립 모드 사용 여부

    private Texture2D selectionTexture;
    private bool isDragging;
    private Vector2 startMousePos;

    // 외부 제어용 프로퍼티 (독립 모드가 아닐 때 사용)
    public bool IsDragging {
        get => isDragging;
        set => isDragging = value;
    }
    public Vector2 StartMousePos {
        get => startMousePos;
        set => startMousePos = value;
    }

    private void Awake() {
        InitializeDragTexture();
    }

    private void Update() {
        // 독립 모드일 때만 자체적으로 입력 처리
        if (!useStandaloneMode) return;

        // 드래그 시작 (터치/마우스 지원)
        if (MouseProvider.WasPressedThisFrame()) {
            startMousePos = MouseProvider.GetScreenPosition();
            isDragging = true;
        }
        // 드래그 종료
        if (MouseProvider.WasReleasedThisFrame()) {
            isDragging = false;
        }
    }

    /// <summary>
    /// 드래그 범위를 화면에 표시
    /// </summary>
    private void OnGUI() {
        if (isDragging) {
            Vector2 currentMousePos = MouseProvider.GetScreenPosition();
            var rect = GetDragRect(startMousePos, currentMousePos);
            rect.y = Screen.height - rect.y - rect.height;
            GUI.DrawTexture(rect, selectionTexture);
        }
    }

    /// <summary>
    /// 드래그 범위 표시를 위한 반투명 텍스처 생성
    /// </summary>
    private void InitializeDragTexture() {
        selectionTexture = new Texture2D(1, 1);
        Color32 fixedColor = new Color32(204, 204, 255, 77);
        selectionTexture.SetPixel(0, 0, fixedColor);
        selectionTexture.Apply();
    }

    /// <summary>
    /// 두 스크린 좌표로부터 드래그 영역 Rect 생성
    /// </summary>
    public static Rect GetDragRect(Vector2 screenPos1, Vector2 screenPos2) {
        var topLeft = Vector2.Min(screenPos1, screenPos2);
        var bottomRight = Vector2.Max(screenPos1, screenPos2);
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }
}
