using UnityEngine;

public class Person : MonoBehaviour
{
    int target;
    float targetX, targetZ;
    float lifeSpan = 60;
    float speed = 1;
    float trashSpawnRate = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        System.Random r = new();
        Transform shirt = transform.Find("Shirt");
        shirt.GetComponent<MeshRenderer>().material.color = new((float) r.NextDouble(), (float) r.NextDouble(), (float) r.NextDouble());

        Transform skin = transform.Find("Skin");
        float skinTone = (float) r.NextDouble();
        skin.GetComponent<MeshRenderer>().material.color = new(skinTone, skinTone, skinTone);
    }

    public void SetTarget(int t)
    {
        target = t;
        int x = t % 10;
        int y = t / 10;
        targetX = 4.5f - x;
        targetZ = 2 * y - 1;
    }

    void Update()
    {
        lifeSpan -= Time.deltaTime;
        if (lifeSpan > 0) {
            while (transform.localPosition.z < targetZ) {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(0, 0, speed * Time.deltaTime), Quaternion.identity);
                return;
            }
            while (transform.localPosition.x > targetX) {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(-speed * Time.deltaTime, 0, 0), Quaternion.identity);
                return;
            }
            if (lifeSpan <= Time.deltaTime && Random.Range(0, 1f) < trashSpawnRate) GameState.Instance.trashSpawner.SpawnTrash(transform.position.x, transform.position.z);
        }
        else
        {
            while (transform.localPosition.x > -6.5) {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(-speed * Time.deltaTime, 0, 0), Quaternion.identity);
                return;
            }
            while (transform.localPosition.z > -3) {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(0, 0, - speed * Time.deltaTime), Quaternion.identity);
                return;
            }
            Destroy(gameObject);
            GameState.Instance.FreeSeat(target);
        }
    }
}
