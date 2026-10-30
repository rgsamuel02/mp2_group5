using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A 1x1 grid-placeable viola that plays a song in the cafe once placed down on the grid.
// Cannot be picked up or placed onto the grid until Goal 3 (Customer Satisfaction of 20) is unlocked.
[RequireComponent(typeof(GridOccupant))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(GrabbableItem))]
[RequireComponent(typeof(AudioSource))]
public class BryceViola : MonoBehaviour
{
    public static readonly HashSet<BryceViola> All = new HashSet<BryceViola>();

    public static bool IsUnlocked { get; private set; }

    [Tooltip("Assign the cafe song AudioClip here (or on the AudioSource component).")]
    public AudioClip song;
    public AudioSource audioSource;

    GridOccupant occupant;
    Rigidbody body;
    XRGrabInteractable grab;
    GrabbableItem grabbable;
    bool wasPlacedLastFrame;

    public bool IsPlacedOnGrid => occupant && occupant.isPlaced;

    public static bool AnyPlacedOnGrid
    {
        get
        {
            foreach (var v in All)
                if (v && v.IsPlacedOnGrid) return true;
            return false;
        }
    }

    public static void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        foreach (var v in Object.FindObjectsByType<BryceViola>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (v) v.ApplyLockState();
        }
    }

    void Awake()
    {
        occupant = GetComponent<GridOccupant>();
        occupant.footprint = Vector2Int.one;

        body = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        grabbable = GetComponent<GrabbableItem>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
        }

        grab.selectExited.AddListener(OnReleased);
        ApplyLockState();
    }

    void OnEnable()
    {
        All.Add(this);
        wasPlacedLastFrame = false;
        ApplyLockState();
        if (IsPlacedOnGrid)
            PlaySong();
    }

    void OnDisable() => All.Remove(this);

    void OnDestroy()
    {
        if (grab) grab.selectExited.RemoveListener(OnReleased);
    }

    public void ApplyLockState()
    {
        if (!occupant) occupant = GetComponent<GridOccupant>();
        if (!body) body = GetComponent<Rigidbody>();
        if (!grab) grab = GetComponent<XRGrabInteractable>();
        if (!grabbable) grabbable = GetComponent<GrabbableItem>();

        if (!IsUnlocked)
        {
            if (grab) grab.enabled = false;
            if (grabbable) grabbable.enabled = false;
            if (body)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }
        else
        {
            if (grab) grab.enabled = true;
            if (grabbable) grabbable.enabled = true;
            if (body && !IsPlacedOnGrid && (!grab || !grab.isSelected))
            {
                body.isKinematic = false;
                body.useGravity = true;
            }
        }
    }

    void Update()
    {
        bool placed = IsPlacedOnGrid;
        if (placed && !wasPlacedLastFrame)
            PlaySong();
        wasPlacedLastFrame = placed;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (IsPlacedOnGrid)
            PlaySong();
    }

    public void PlaySong()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) return;

        if (song) audioSource.clip = song;
        audioSource.loop = true;
        if (audioSource.clip && !audioSource.isPlaying)
            audioSource.Play();
    }
}
