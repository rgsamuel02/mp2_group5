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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_Parcels.Clear();
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
            OnSceneLoaded(active, LoadSceneMode.Single);
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

            TransportReceiver receiver = null;
            if (!p.arrived)
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
            if (receiver != null)
                receiver.Arrived(p.go);
        }
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
        XRInteractionManager manager = null;
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
