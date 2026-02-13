using UnityEngine;

public class SafeArea : MonoBehaviour
{
    Rect currentSafeArea;

    void Start()
    {
        currentSafeArea = Screen.safeArea;
        ApplySafeArea(currentSafeArea);
    }

    void Update()
    {
        var safeArea = Screen.safeArea;
        if (safeArea != currentSafeArea)
        {
            currentSafeArea = safeArea;
            ApplySafeArea(safeArea);
        }
    }

    void ApplySafeArea(Rect r)
    {
        Vector2 anchorMin = r.position;
        Vector2 anchorMax = r.position + r.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        var panel = gameObject.GetComponent<RectTransform>();
        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;
    }
}
