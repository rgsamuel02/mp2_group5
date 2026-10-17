using UnityEngine;

// Turns a world-space label to face the camera.
public class Billboard : MonoBehaviour
{
    Camera cam;

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;
        // A world-space canvas reads correctly with its forward pointing away from the viewer.
        var away = transform.position - cam.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
    }
}
