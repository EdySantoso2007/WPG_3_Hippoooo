using UnityEngine;

public class CameraTargetRegister : MonoBehaviour
{
    void Start()
    {
        DynamicMultiplayerCamera cam = FindFirstObjectByType<DynamicMultiplayerCamera>();

        if (cam != null)
        {
            cam.AddTarget(this.transform);
        }
    }

    void OnDestroy()
    {
        DynamicMultiplayerCamera cam = FindFirstObjectByType<DynamicMultiplayerCamera>();

        if (cam != null)
        {
            cam.RemoveTarget(this.transform);
        }
    }
}