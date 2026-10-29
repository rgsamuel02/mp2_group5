using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A 1x1 grid-placeable viola that plays a song in the cafe once placed down on the grid.
[RequireComponent(typeof(GridOccupant))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(GrabbableItem))]
[RequireComponent(typeof(AudioSource))]
public class BryceViola : MonoBehaviour
{
    public static readonly HashSet<BryceViola> All = new HashSet<BryceViola>();

    [Tooltip("Assign the cafe song AudioClip here (or on the AudioSource component).")]
    public AudioClip song;
    public AudioSource audioSource;

    GridOccupant occupant;
    XRGrabInteractable grab;
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

    void Awake()
    {
        occupant = GetComponent<GridOccupant>();
        occupant.footprint = Vector2Int.one;

        grab = GetComponent<XRGrabInteractable>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
        }

        grab.selectExited.AddListener(OnReleased);
    }

    void OnEnable() => All.Add(this);

    void OnDisable() => All.Remove(this);

    void OnDestroy()
    {
        if (grab) grab.selectExited.RemoveListener(OnReleased);
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
