using System.Collections;
using UnityEngine;

public class MenuFlyout : MonoBehaviour
{
    public Vector2 closedPosition, openPosition;
    public float speed;
    public RectTransform rect;
    public bool closed = true;
    public CanvasGroup flyoutGroup;
    private void Start()
    {
        closed = true;
        rect.anchoredPosition = closedPosition;
    }
    public void ToggleFlyout()
    {
        StopCoroutine(Flyout());
        StartCoroutine(Flyout());
    }
    public IEnumerator Flyout()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * speed;
            rect.anchoredPosition = Vector2.Lerp(closedPosition, openPosition, closed ? t : (1 - t));
            yield return null;
        }
        
        closed = !closed;
        flyoutGroup.blocksRaycasts = !closed;
        flyoutGroup.interactable = !closed;
        
    }
}
