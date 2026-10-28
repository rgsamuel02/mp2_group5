using UnityEngine;

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
    public ParticleSystem arriveEffect;
    public AudioSource arriveSound;

    public Vector3 ArrivalPoint => transform.position + Vector3.up * dropHeight;

    void Awake() => TransportTunnel.Tint(colorRenderers, color);

    // Shows the route color in the Editor too.
    void OnValidate() => TransportTunnel.Tint(colorRenderers, color);

    public void Arrived(GameObject item)
    {
        if (arriveEffect != null)
            arriveEffect.Play();
        if (arriveSound != null)
            arriveSound.Play();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(ArrivalPoint, 0.08f);
    }
}
