using UnityEngine;
using UnityEngine.UI;

public class BuyFlower : MonoBehaviour
{
    public Button b;
    public GameObject flowerPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        b.onClick.AddListener(SpawnFlower);
    }

    void SpawnFlower()
    {
        if (GameState.Instance.WithdrawFlower())
            Instantiate(flowerPrefab, new Vector3(0, 1, 0), Quaternion.identity);
    }
}
