using UnityEngine.InputSystem;
using UnityEngine;

/// <summary>
/// 2D 터치/마우스 입력 제공자
/// </summary>
public static class MouseProvider
{
    /// <summary>
    /// 현재 포인터(마우스/터치) 위치의 월드 좌표 반환
    /// </summary>
    public static Vector2 GetWorldPosition()
    {
        Vector2 screenPos = GetScreenPosition();
        return Camera.main.ScreenToWorldPoint(screenPos);
    }
    
    /// <summary>
    /// 현재 포인터(마우스/터치) 스크린 좌표 반환
    /// </summary>
    public static Vector2 GetScreenPosition()
    {
        // 터치 입력 우선 체크
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }
        
        // 마우스 입력
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        
        return Vector2.zero;
    }
    
    /// <summary>
    /// 2D Raycast로 특정 레이어의 오브젝트 감지
    /// </summary>
    public static RaycastHit2D GetHitInfo2D(LayerMask layerMask)
    {
        Vector2 worldPos = GetWorldPosition();
        return Physics2D.Raycast(worldPos, Vector2.zero, 0f, layerMask);
    }
    
    /// <summary>
    /// 2D Raycast로 모든 레이어의 오브젝트 감지
    /// </summary>
    public static RaycastHit2D GetHitInfo2D()
    {
        Vector2 worldPos = GetWorldPosition();
        return Physics2D.Raycast(worldPos, Vector2.zero);
    }
    
    /// <summary>
    /// 포인터 아래의 모든 2D 오브젝트 감지
    /// </summary>
    public static RaycastHit2D[] GetAllHits2D(LayerMask layerMask)
    {
        Vector2 worldPos = GetWorldPosition();
        return Physics2D.RaycastAll(worldPos, Vector2.zero, 0f, layerMask);
    }
    
    /// <summary>
    /// 클릭/터치 중인지 확인
    /// </summary>
    public static bool IsPressed()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }
        
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 이번 프레임에 클릭/터치가 시작되었는지 확인
    /// </summary>
    public static bool WasPressedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }
        
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 이번 프레임에 클릭/터치가 끝났는지 확인
    /// </summary>
    public static bool WasReleasedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            return true;
        }
        
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            return true;
        }
        
        return false;
    }
}