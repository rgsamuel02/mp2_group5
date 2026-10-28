using System.Collections.Generic;
using UnityEngine;

public class ShowVases : MonoBehaviour
{
    public List<GameObject> tempVases;
    public static List<GameObject> vases;
    static bool vasesAlreadySpawned = false;

    void Start()
    {
        if (!vasesAlreadySpawned) {
            vases = new(tempVases);
            for (int i = 0; i < vases.Count; i++)
            {
                vases[i].transform.SetParent(null);
                DontDestroyOnLoad(vases[i]);
            }
            vasesAlreadySpawned = true;
        }
        else
        {
            foreach (GameObject v in tempVases)
            {
                Destroy(v);
            }
        }
    }

    public static void MakeVasesActive()
    {
        for (int i = 0; i < vases.Count; i++)
        {
            vases[i].SetActive(true);
        }
    }
}
