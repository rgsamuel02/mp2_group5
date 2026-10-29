using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Arrival point for TransportTunnel deliveries. Put it in the destination scene (e.g. on a crate or a spot on a
/// counter) and give it an id; a tunnel in another scene with the same Receiver Id sends objects here.
/// </summary>
public class TransportReceiver : MonoBehaviour
{
    public string receiverId = "Kitchen";
    [Tooltip("Objects appear this far above the receiver and drop in.")]
    public float dropHeight = 0.3f;
    public Color color = new Color(0.2f, 0.8f, 1f);
    [Tooltip("Optional renderers tinted with the receiver's color (matches the sending tunnel).")]
    public Renderer[] colorRenderers;
    [Tooltip("Give arriving objects a Rigidbody, collider and grab component if they have none, so the player can pick them up.")]
    public bool ensureGrabbable = true;
    [Tooltip("Optional sign text, filled in from the id (e.g. \"KITCHEN\nRECEIVING\").")]
    public UnityEngine.UI.Text label;
    public ParticleSystem arriveEffect;
    public AudioSource arriveSound;
    [Tooltip("Runs for every object that arrives here (check its name to decide what to do with it).")]
    public UnityEvent<GameObject> onArrived = new UnityEvent<GameObject>();

    public Vector3 ArrivalPoint => transform.position + Vector3.up * dropHeight;

    void Awake() => Refresh();

    // Shows the route color and sign in the Editor too.
    void OnValidate() => Refresh();

    void Refresh()
    {
        TransportTunnel.Tint(colorRenderers, color);
        TransportTunnel.SetLabel(label, (receiverId ?? "").ToUpperInvariant() + " RECEIVING", color);
    }

    public void Arrived(GameObject item)
    {
        if (arriveEffect != null)
            arriveEffect.Play();
        if (arriveSound != null)
            arriveSound.Play();
        onArrived?.Invoke(item);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(ArrivalPoint, 0.08f);
    }
}
