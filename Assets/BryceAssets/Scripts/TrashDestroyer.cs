using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TrashDestroyer : MonoBehaviour
{
    bool firstTrashDestroyed = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        XRSocketInteractor ir = gameObject.GetComponent<XRSocketInteractor>();
        ir.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (firstTrashDestroyed && !GameState.Instance.menu.IsOpen()) {
            GameState.Instance.menu.LoadMenu("Good job cleaning up. Your customers will appreciate that and pay more to listen to your orchestra.\n\nYou also got some material for recycling. You can use that to fix instruments\n\nPress B to close this menu.");
        }
        // Get the interactable object that entered the socket
        GameObject socketedObject = args.interactableObject.transform.gameObject;

        // Destroy the game object
        Destroy(socketedObject);

        GameState.Instance.DestroyTrash();
    }
}
