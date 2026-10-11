using System.Collections.Generic;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    public List <GameObject> trashPrefabs;
    public GameObject AudienceArea;


    public void Start()
    {
        System.Random r = new();
        for (int i = 0; i < 10; i++) {
            int location = r.Next(50);
            int x = location % 10;
            int y = location / 10;
            Instantiate(trashPrefabs[r.Next(trashPrefabs.Count)], AudienceArea.transform, false).transform.localPosition = new(4.5f - x, 0, 2 * y - 1);
        }
        Destroy(gameObject);
    }
}
