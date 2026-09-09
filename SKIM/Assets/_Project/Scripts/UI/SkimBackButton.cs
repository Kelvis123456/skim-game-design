using UnityEngine;
using UnityEngine.UI;

// Returns any sub-screen to the main menu. Lives as a component rather than an
// editor-time listener so the wiring survives scene serialization.
[RequireComponent(typeof(Button))]
public class SkimBackButton : MonoBehaviour
{
    void Start()
    {
        var menu = GetComponentInParent<MainMenuController>(true);
        if (menu == null) return;
        GetComponent<Button>().onClick.AddListener(menu.ShowMainPanel);
    }
}
