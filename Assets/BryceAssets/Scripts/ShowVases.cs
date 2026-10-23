using System.Collections.Generic;
using UnityEngine;

public class ShowVases : MonoBehaviour
{
    public List<GameObject> tempVases;
    public static List<GameObject> vases;

    void Start()
    {
        vases = new(tempVases);
    }

    public static void MakeVasesActive()
    {
        for (int i = 0; i < vases.Count; i++)
        {
            vases[i].SetActive(true);
        }
    }
}
