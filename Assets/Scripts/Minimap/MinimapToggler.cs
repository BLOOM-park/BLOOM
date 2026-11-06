using UnityEngine;

public class MinimapToggler : MonoBehaviour
{
    public GameObject MinimapObject;

    public GameObject FullscreenMapObject;

    void Update()
    {
        if (PauseMenu.isPaused) return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (MinimapObject != null && FullscreenMapObject != null)
            {
                bool isMinimapActive = MinimapObject.activeSelf;
                MinimapObject.SetActive(!isMinimapActive);
                FullscreenMapObject.SetActive(isMinimapActive);
            }
        }
    }
}
