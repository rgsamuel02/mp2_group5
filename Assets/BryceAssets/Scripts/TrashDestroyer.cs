using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TrashDestroyer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        XRSocketInteractor ir = gameObject.GetComponent<XRSocketInteractor>();
        ir.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Get the interactable object that entered the socket
        GameObject socketedObject = args.interactableObject.transform.gameObject;

        // Destroy the game object
        Destroy(socketedObject);

        GameState.Instance.DestroyTrash();
    }
}
