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
        print("Toggled Flyout");
        StopCoroutine(Flyout());
        StartCoroutine(Flyout());
    }
    public IEnumerator Flyout()
    {
        print("Wooosh says the menu!");
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * speed;
            rect.anchoredPosition = Vector2.Lerp(closedPosition, openPosition, closed ? t : (1 - t));
            print(t);
            yield return null;
        }
        
        closed = !closed;
        flyoutGroup.blocksRaycasts = !closed;
        flyoutGroup.interactable = !closed;
        
    }
}
