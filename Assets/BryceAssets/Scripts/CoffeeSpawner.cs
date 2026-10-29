using UnityEngine;

public class CoffeeSpawner : MonoBehaviour
{
    public GameObject spawnPrefab;
    public Vector3 position;

    void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.name == "Left Controller" || other.gameObject.name == "Right Controller") && (!CoffeeTutorial.isCompleted || GameState.Instance.WithdrawCoffee())) {
            Instantiate(spawnPrefab, position, Quaternion.identity);
        }
    }
}
