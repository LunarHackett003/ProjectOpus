using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public struct MenuSection
    {
        public string name;
        public Canvas canvas;
        public Transform worldCameraPosition;
    }
    public List<MenuSection> sections;
    public void SetActiveCanvas(Canvas canvas)
    {
        for (int i = 0; i < sections.Count; i++)
        {
            if (sections[i].canvas == canvas)
            {
                canvas.gameObject.SetActive(true);
            }
            else
            {
                canvas.gameObject.SetActive(false);
            }
        }
    }
    public void SetMainMenuActive()
    {
        SetActiveCanvas(sections.Find(x => x.name == "MainMenu").canvas);
    }

}
