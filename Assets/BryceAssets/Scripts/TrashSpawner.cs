using System.Collections.Generic;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    public List <GameObject> trashPrefabs;
    public GameObject AudienceArea;
    bool firstSpawn = true;


    public void Start()
    {
        System.Random r = new();
        for (int i = 0; i < 10; i++) {
            int location = r.Next(50);
            int x = location % 10;
            int y = location / 10;
            Instantiate(trashPrefabs[Random.Range(0, 3)], AudienceArea.transform, false).transform.localPosition = new(4.5f - x, 0, 2 * y - 1);
        }
    }

    public void SpawnTrash(float x, float z)
    {
        if (firstSpawn)
        {
            GameState.Instance.menu.LoadMenu("Some rude audience members will leave trash behind.\n\nBetter clean it up to make sure people have a good time or you won't get paid as much.\n\nI bet you get material from it though.\n\nPress B to close this menu.");
            firstSpawn = false;
        }
        Instantiate(trashPrefabs[Random.Range(0, 3)], GameState.Instance.parent).transform.position = new Vector3(x, 0, z);
        GameState.Instance.CreateTrash();
    }
}
