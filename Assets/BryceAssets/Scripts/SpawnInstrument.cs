using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SpawnInstrument : MonoBehaviour
{
    public enum Instrument
    {
        TROMBONE,
        VIOLIN,
        TUBA,
        HORN,
        CLARINET,
        CELLO,
        BASS
    };

    private GameObject selected = null;

    public InputActionReference placeButton;
    public XRRayInteractor raycast;

    private float gridSize = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        placeButton.action.started += (ctx) =>
        {
            Place();
        };
        placeButton.action.Disable();
    }

    // Update is called once per frame
    public void Select(GameObject prefab)
    {
        selected = Instantiate(prefab);
        placeButton.action.Enable();
    }

    void Update()
    {
        if (selected == null) return;
        raycast.TryGetCurrent3DRaycastHit(out RaycastHit hit);
        selected.transform.SetPositionAndRotation(GetNearestGridPoint(hit.point), selected.GetComponent<Transform>().rotation * Quaternion.identity);
    }
    
    Vector3 GetNearestGridPoint(Vector3 p)
    {
        Vector3 ret;
        ret.x = (float) Math.Round(p.x / gridSize) * gridSize; 
        ret.y = selected.GetComponent<Transform>().position.y;
        ret.z = (float) Math.Round(p.z / gridSize) * gridSize;
        return ret;
    }

    bool Place()
    {
        selected = null;
        placeButton.action.Disable();
        return true;
    }
}
