using UnityEngine;

public static class RendererBounds
{
    // Combined world bounds of every renderer under `go`; false if it has none.
    public static bool TryGet(GameObject go, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }
}
