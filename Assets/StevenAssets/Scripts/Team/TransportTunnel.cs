using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A crate or tunnel that sends any physics object put into it to another scene.
///
/// Setup: put this on the crate/tunnel object (or an empty child at its opening) with a trigger collider covering the
/// mouth. Set Destination Scene, then either a Receiver Id (a TransportReceiver in that scene) or a Destination
/// Position. Works facing up (crate) or sideways (tunnel): only the trigger's placement matters.
///
/// An object is sent when it is inside the trigger and nobody is holding it, so players can drop or throw things in.
/// It disappears and waits (see DeliveryCarrier) until the player next enters the destination scene, then drops in at
/// the receiver. Destination Scene may be the start of a name ("BryceSceneLvl" matches every Orchestra level).
/// </summary>
[RequireComponent(typeof(Collider))]
public class TransportTunnel : MonoBehaviour
{
    [Header("Where to send")]
    public string destinationScene = "StevenScene";
    [Tooltip("Id of a TransportReceiver in the destination scene. Leave empty to use Destination Position.")]
    public string receiverId = "";
    public Vector3 destinationPosition;
    [Tooltip("Random sideways spread so several items don't land inside each other.")]
    public float spread = 0.12f;

    [Header("What to send")]
    [Tooltip("Only send objects whose name contains one of these (case-insensitive). Empty = any object with a Rigidbody.")]
    public List<string> acceptNameContains = new List<string>();

    [Header("Look and feedback")]
    public Color tunnelColor = new Color(0.2f, 0.8f, 1f);
    [Tooltip("Renderers tinted with the tunnel color (color-code each route).")]
    public Renderer[] colorRenderers;
    public ParticleSystem sendEffect;
    public AudioSource sendSound;

    readonly HashSet<GameObject> m_InFlight = new HashSet<GameObject>();

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null)
            c.isTrigger = true;
    }

    void Awake()
    {
        foreach (var c in GetComponents<Collider>())
            c.isTrigger = true;
        Tint(colorRenderers, tunnelColor);
    }

    // Shows the route color in the Editor too.
    void OnValidate() => Tint(colorRenderers, tunnelColor);

    void OnTriggerEnter(Collider other) => Consider(other);

    // Stay: an object carried into the tunnel is sent as soon as the player lets go of it.
    void OnTriggerStay(Collider other) => Consider(other);

    void Consider(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null)
            return;
        var go = rb.gameObject;
        if (m_InFlight.Contains(go) || !Accepts(go) || IsHeld(go))
            return;
        Send(go, rb);
    }

    bool Accepts(GameObject go)
    {
        // Never send the player, their hands or this tunnel.
        if (go == gameObject || go.transform.IsChildOf(transform))
            return false;
        if (go.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null || go.GetComponentInParent<CharacterController>() != null)
            return false;
        if (acceptNameContains == null || acceptNameContains.Count == 0)
            return true;
        foreach (var s in acceptNameContains)
            if (!string.IsNullOrEmpty(s) && go.name.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }

    static bool IsHeld(GameObject go)
    {
        foreach (var i in go.GetComponentsInChildren<XRBaseInteractable>())
            if (i.isSelected)
                return true;
        return false;
    }

    void Send(GameObject go, Rigidbody rb)
    {
        m_InFlight.Add(go);
        if (sendEffect != null)
            sendEffect.Play();
        if (sendSound != null)
            sendSound.Play();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        DeliveryCarrier.Send(go, destinationScene, receiverId, destinationPosition, spread);
        m_InFlight.Remove(go);
    }

    public static void Tint(Renderer[] renderers, Color color)
    {
        if (renderers == null)
            return;
        foreach (var r in renderers)
        {
            if (r == null)
                continue;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = tunnelColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        if (string.IsNullOrEmpty(receiverId))
            Gizmos.DrawLine(transform.position, destinationPosition);
    }
}
