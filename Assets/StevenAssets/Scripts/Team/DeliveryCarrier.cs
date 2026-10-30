using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Keeps objects sent through a TransportTunnel alive across scene loads (the team's TeleportationManager loads one
/// scene at a time). A sent object is hidden and kept aside; it appears at the receiver the next time its destination
/// scene loads, stays there while the player is in that scene, and is hidden again (where it was left) when they leave.
/// Once the object is put into something in that scene (parented to it), that scene owns it.
///
/// From code (no crate needed): DeliveryCarrier.Send(Instantiate(prefab), "StevenScene", "Kitchen", Vector3.zero, 0.1f);
/// To react to what arrives: DeliveryCarrier.Delivered += (item, sceneName) => { ... }, or a receiver's On Arrived event.
/// </summary>
public static class DeliveryCarrier
{
    class Parcel
    {
        public GameObject go;
        public string scene;          // destination scene name (or its start, e.g. "BryceSceneLvl")
        public string receiverId;
        public Vector3 fallbackPosition;
        public float spread;
        public bool arrived;
    }

    static readonly List<Parcel> s_Parcels = new List<Parcel>();

    /// <summary>Raised when an object first arrives in its destination scene: (object, scene name).</summary>
    public static event System.Action<GameObject, string> Delivered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_Parcels.Clear();
        Delivered = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>Number of objects still waiting to arrive in a scene.</summary>
    public static int Waiting(string scene)
    {
        Prune();
        var n = 0;
        foreach (var p in s_Parcels)
            if (!p.arrived && Matches(p.scene, scene))
                n++;
        return n;
    }

    public static void Send(GameObject go, string destinationScene, string receiverId, Vector3 fallbackPosition, float spread)
    {
        if (go == null)
            return;
        Prune();
        s_Parcels.RemoveAll(p => p.go == go);
        go.transform.SetParent(null, true);
        Object.DontDestroyOnLoad(go);
        go.SetActive(false);
        s_Parcels.Add(new Parcel
        {
            go = go, scene = destinationScene, receiverId = receiverId,
            fallbackPosition = fallbackPosition, spread = spread
        });

        // Destination already open (e.g. a crate sending within the same scene): deliver now.
        var active = SceneManager.GetActiveScene();
        if (Matches(destinationScene, active.name))
            Deliver(s_Parcels[s_Parcels.Count - 1], active);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Prune();
        foreach (var p in s_Parcels)
        {
            var here = Matches(p.scene, scene.name);
            if (!here)
            {
                // A different scene took over (single loading): keep the parcel hidden where it was left.
                if (mode == LoadSceneMode.Single)
                    p.go.SetActive(false);
                continue;
            }

            Deliver(p, scene);
        }
    }

    static void Deliver(Parcel p, Scene scene)
    {
        TransportReceiver receiver = null;
        var firstArrival = !p.arrived;
        if (firstArrival)
        {
            receiver = FindReceiver(scene, p.receiverId);
            var target = receiver != null ? receiver.ArrivalPoint : p.fallbackPosition;
            var jitter = Random.insideUnitCircle * p.spread;
            p.go.transform.position = target + new Vector3(jitter.x, 0f, jitter.y);
            p.arrived = true;
        }

        var rb = p.go.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Rebind(p.go, scene);
        p.go.SetActive(true);
        // Added while active so renderer bounds are valid for the collider size.
        if (firstArrival && receiver != null && receiver.ensureGrabbable && MakeGrabbable(p.go))
            Rebind(p.go, scene);
        if (receiver != null)
            receiver.Arrived(p.go);
        if (firstArrival)
            Delivered?.Invoke(p.go, scene.name);
    }

    /// <summary>Objects from other sims (e.g. instruments) may have no physics or grab component.</summary>
    static bool MakeGrabbable(GameObject go)
    {
        var changed = false;
        if (go.GetComponentInChildren<Collider>() == null)
        {
            var box = go.AddComponent<BoxCollider>();
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers)
                    b.Encapsulate(r.bounds);
                box.center = go.transform.InverseTransformPoint(b.center);
                var s = go.transform.lossyScale;
                box.size = new Vector3(b.size.x / Mathf.Max(0.0001f, Mathf.Abs(s.x)), b.size.y / Mathf.Max(0.0001f, Mathf.Abs(s.y)), b.size.z / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
            }
            changed = true;
        }
        if (go.GetComponent<Rigidbody>() == null)
        {
            go.AddComponent<Rigidbody>();
            changed = true;
        }
        if (go.GetComponentInChildren<XRBaseInteractable>() == null)
        {
            go.AddComponent<XRGrabInteractable>();
            changed = true;
        }
        return changed;
    }

    /// <summary>Drops parcels that were destroyed (used up) or moved into a scene (placed into something there).</summary>
    static void Prune()
    {
        // Parenting it to something in a scene moves it into that scene, which then owns it.
        s_Parcels.RemoveAll(p => p.go == null || (p.arrived && p.go.scene.name != "DontDestroyOnLoad"));
    }

    static bool Matches(string destination, string sceneName) =>
        !string.IsNullOrEmpty(destination) && sceneName.StartsWith(destination, System.StringComparison.OrdinalIgnoreCase);

    static TransportReceiver FindReceiver(Scene scene, string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<TransportReceiver>(true))
                if (r.receiverId == id)
                    return r;
        Debug.LogWarning("[DeliveryCarrier] No TransportReceiver with id '" + id + "' in " + scene.name + "; using Destination Position.");
        return null;
    }

    /// <summary>Grabbable objects must use the current scene's interaction manager, or they can't be picked up.</summary>
    static void Rebind(GameObject go, Scene scene)
    {
        // Use the manager the player's hands are registered with, so the item can always be grabbed.
        XRInteractionManager manager = null;
        foreach (var hand in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(FindObjectsInactive.Exclude))
        {
            if (hand != null && hand.isActiveAndEnabled && hand.interactionManager != null)
            {
                manager = hand.interactionManager;
                break;
            }
        }
        if (manager == null)
        foreach (var root in scene.GetRootGameObjects())
        {
            manager = root.GetComponentInChildren<XRInteractionManager>();
            if (manager != null)
                break;
        }
        if (manager == null)
            manager = Object.FindFirstObjectByType<XRInteractionManager>();
        if (manager == null)
            return;
        foreach (var i in go.GetComponentsInChildren<XRBaseInteractable>(true))
            i.interactionManager = manager;
    }
}
