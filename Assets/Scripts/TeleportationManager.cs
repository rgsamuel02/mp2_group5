using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeleportationManager : MonoBehaviour
{
    public InputActionReference StickPressed;
    public InputActionReference StickPosition;
    public Image StickRepresentation;
    public TextMeshProUGUI north, east, south, west;
    public InputActionReference confirm;
    string SelectedScene = "";
    static string CurrentScene = "Orchestra";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.SetActive(false);
        StickPressed.action.performed += OpenMenu;
        StickPressed.action.Enable();
        StickPosition.action.Enable();
        confirm.action.Enable();
    }

    private void OpenMenu(InputAction.CallbackContext context)
    {
        StickPressed.action.performed -= OpenMenu;
        StickPosition.action.performed += StickMoved;
        StickPosition.action.Enable();
        StickPressed.action.performed += CloseMenu;
        StickPressed.action.Enable();
        gameObject.SetActive(true);
    }

    private void CloseMenu(InputAction.CallbackContext context)
    {
        StickPosition.action.performed -= StickMoved;
        StickPressed.action.performed -= CloseMenu;
        StickPressed.action.performed += OpenMenu;
        gameObject.SetActive(false);
    }

    private void StickMoved(InputAction.CallbackContext context)
    {
        south.color = new Color(0, 0, 0);
        north.color = new Color(0, 0, 0);
        east.color = new Color(0, 0, 0);
        west.color = new Color(0, 0, 0);
        Vector2 stickPos = context.ReadValue<Vector2>() * 200;
        StickRepresentation.transform.localPosition = stickPos;
        if (stickPos.magnitude < 0.5) return; // deadzone prevents accidental inputs
        if (Math.Abs(stickPos.x) < Math.Abs(stickPos.y) && stickPos.y > 0) {
            north.color = new Color(0, 1, 0);
            confirm.action.performed += LoadLevel;
            SelectedScene = "Cafe";
        }
        if (Math.Abs(stickPos.x) < Math.Abs(stickPos.y) && stickPos.y < 0) {
            south.color = new Color(0, 1, 0);
            confirm.action.performed += LoadLevel;
            SelectedScene = "Orchestra";
        }
        if (Math.Abs(stickPos.x) >= Math.Abs(stickPos.y) && stickPos.x > 0) {
            east.color = new Color(0, 1, 0);
            confirm.action.performed += LoadLevel;
            SelectedScene = "Garden";
        }
        if (Math.Abs(stickPos.x) >= Math.Abs(stickPos.y) && stickPos.x < 0) {
            west.color = new Color(0, 1, 0);
            confirm.action.performed += LoadLevel;
            SelectedScene = "Kitchen";
        }
    }

    private void LoadLevel(InputAction.CallbackContext context)
    {
        CloseMenu(context);
        confirm.action.performed -= LoadLevel;
        if (SelectedScene.Equals(CurrentScene)) return;
        StickPressed.action.performed -= OpenMenu;

        if (CurrentScene.Equals("Cafe")) UnloadCafe();
        else if (CurrentScene.Equals("Orchestra")) UnloadOrchestra();
        else if (CurrentScene.Equals("Garden")) UnloadGarden();
        else if (CurrentScene.Equals("Kitchen")) UnloadKitchen();

        if (SelectedScene.Equals("Cafe")) LoadCafe();
        else if (SelectedScene.Equals("Orchestra")) LoadOrchestra();
        else if (SelectedScene.Equals("Garden")) LoadGarden();
        else if (SelectedScene.Equals("Kitchen")) LoadKitchen();
    }

    void UnloadCafe()
    {
        
    }

    void LoadCafe() {
        CurrentScene = "Cafe";
        Debug.Log("Loading Cafe");
        SceneManager.LoadScene("Rishabh Scene");
    }

    void UnloadOrchestra()
    {
        // save instruments
        Debug.Log("Leaving Orchestra");
        Instrument[] instruments = FindObjectsByType<Instrument>();
        foreach (Instrument i in instruments) 
        {
            i.gameObject.SetActive(false);
            DontDestroyOnLoad(i.gameObject);
        }

        // save trash
        GameObject[] trash = GameObject.FindGameObjectsWithTag("Trash");
        GameObject temp = new GameObject("OrchestraTrashHolder");
        foreach (GameObject t in trash)
        {
            t.transform.SetParent(temp.transform);
            t.SetActive(false);
        }
        DontDestroyOnLoad(temp);

        // save vases
        GameObject[] vases = GameObject.FindGameObjectsWithTag("Vase");
        foreach (GameObject v in vases)
        {
            v.GetComponent<Vase>().CleanUp();
        }

        // save audience
        GameState.Instance.SaveAudience();

        GameState.Instance.gameObject.SetActive(false);
    }

    void LoadOrchestra() {
        int level = GameState.Instance == null ? 1 : GameState.Instance.level;
        CurrentScene = "Orchestra";

        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.SetActiveScene(scene);

            GameState.Instance.gameObject.SetActive(true);
            Instrument[] instruments = FindObjectsByType<Instrument>(FindObjectsInactive.Include);
            SceneManager.SetActiveScene(scene);
            foreach (Instrument i in instruments)
            {
                i.gameObject.SetActive(true);
                SceneManager.MoveGameObjectToScene(i.gameObject, scene);
            }

            GameObject temp = GameObject.Find("OrchestraTrashHolder");
            for (int i = temp.transform.childCount - 1; i >= 0; i--)
            {
                // Access the child GameObject
                Transform trash = temp.transform.GetChild(i);
                trash.SetParent(GameState.Instance.audience.transform);
                trash.gameObject.SetActive(true);
            }
            Destroy(temp);

            if (GameState.Instance.UnlockedGarden())
            {
                foreach (GameObject v in ShowVases.vases) 
                {
                    v.GetComponent<Vase>().StartUp();
                }
            }

            GameState.Instance.LoadAudience();
        }

        SceneManager.sceneLoaded += OnLoaded;
        SceneManager.LoadScene($"BryceSceneLvl{level}");
    }

    void UnloadGarden()
    {
        Scr_Save.Instance.SaveGarden();
    }

    void LoadGarden()
    {
        CurrentScene = "Garden";

        SceneManager.sceneLoaded += OnGardenLoaded;
        SceneManager.LoadScene("SampleScene");
    }

    void OnGardenLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene")
            return;

        SceneManager.sceneLoaded -= OnGardenLoaded;

        Scr_Save saveSystem = FindFirstObjectByType<Scr_Save>();

        saveSystem.LoadGarden();
    }

    void UnloadKitchen()
    {
        
    }

    void LoadKitchen() {
        CurrentScene = "Kitchen";
        SceneManager.LoadScene("StevenScene");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
