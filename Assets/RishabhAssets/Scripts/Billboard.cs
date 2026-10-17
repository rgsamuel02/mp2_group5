using UnityEngine;

/// <summary>
/// Keeps a world-space label turned toward the player's camera so it's readable from any side.
/// </summary>
public class Billboard : MonoBehaviour
{
    Camera cam;

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;
        // A world-space canvas reads correctly when its forward points away from the viewer.
        var away = transform.position - cam.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
    }
}
