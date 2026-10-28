using UnityEngine;

public class CoffeeSpawner : MonoBehaviour
{
    public GameObject spawnPrefab;
    public Vector3 position;

    void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.name == "Left Controller" || other.gameObject.name == "Right Controller") && GameState.Instance.GetMoney() > 2) {
            GameState.Instance.AddPay(-2);
            Instantiate(spawnPrefab, position, Quaternion.identity);
        }
    }
}
