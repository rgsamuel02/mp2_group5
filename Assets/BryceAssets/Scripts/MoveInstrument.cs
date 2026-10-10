using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class MoveInstrument : MonoBehaviour
{
    private GameObject selected = null;

    public InputActionReference placeButton;
    public XRRayInteractor raycast;
    public Material unplaceableMat;
    private Material selectedMat;
    private Vector3 offset;

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
        selectedMat = selected.GetComponent<MeshRenderer>().material;
        placeButton.action.Enable();
        offset = prefab.GetComponent<Transform>().Find("offset").localPosition;
    }

    void Update()
    {
        if (selected == null) return;
        raycast.TryGetCurrent3DRaycastHit(out RaycastHit hit);
        Vector2 target = GetNearestGridPoint(hit.point);
        if (GameState.Instance.Placeable(selected.GetComponent<Instrument>(), (int) target.x, (int) target.y))
        {
            placeButton.action.Enable();
            selected.GetComponent<MeshRenderer>().material = selectedMat;
        }
        else
        {
            placeButton.action.Disable();
            selected.GetComponent<MeshRenderer>().material = unplaceableMat;
        }
        selected.transform.SetPositionAndRotation(new Vector3((target.x - 14) * gridSize, selected.GetComponent<Transform>().position.y, (-target.y - 1) * gridSize) - offset, selected.GetComponent<Transform>().rotation * Quaternion.identity);
    }
    
    Vector2 GetNearestGridPoint(Vector3 p)
    {
        Vector2 ret;
        ret.x = (float) Math.Round(p.x / gridSize) + 14; 
        ret.y = -(float) Math.Round(p.z / gridSize + 1);
        return ret;
    }

    bool Place()
    {
        Vector3 pos = selected.transform.position + offset;
        int x = (int) Math.Round(pos.x / gridSize + 14);
        int y = (int) -Math.Round(pos.z / gridSize + 1);
        if (GameState.Instance.Place(selected.GetComponent<Instrument>(), x, y)) {
            selected = null;
            placeButton.action.Disable();
            return true;
        }
        return false;
    }
}
