using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Person : MonoBehaviour
{
    int target;
    float targetX, targetZ;
    float lifeSpan = 60;
    float speed = 1;
    float trashSpawnRate = 0.1f;
    int timeToSleep = 400; // Expected value for how long it will take for them to fall asleep. The exact distribution is difficult to describe.
    float spawnPrice;
    int vipRarity = 10; // One in this many people are VIPs
    bool vip = false;
    public bool isTutorial = false;

    XRSocketInteractor held;
    bool caffeinated = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Random.Range(0, vipRarity) == 0 && GameState.Instance.level >= 3) 
        {
            vip = true;
            transform.Find("VIP").gameObject.SetActive(true);
        }
        held = gameObject.GetComponent<XRSocketInteractor>();
        held.selectEntered.AddListener(StartSelect);

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
        if (lifeSpan > 10 && Random.Range(0, (int) (timeToSleep / Time.deltaTime)) == 0 && !caffeinated && GameState.Instance.level > 1 && !isTutorial)
        {
            transform.Find("Canvas").gameObject.SetActive(true);
        }
        lifeSpan -= Time.deltaTime;
        if (lifeSpan > 0) {
            if (transform.localPosition.z < targetZ) {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(0, 0, speed * Time.deltaTime), Quaternion.identity);
                return;
            }
            if (transform.localPosition.x > targetX) {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(-speed * Time.deltaTime, 0, 0), Quaternion.identity);
                return;
            }
            if (lifeSpan <= Time.deltaTime && Random.Range(0, 1f) < trashSpawnRate) GameState.Instance.trashSpawner.SpawnTrash(transform.position.x, transform.position.z);
        }
        else
        {
            if (transform.localPosition.x > -6.5) {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(-speed * Time.deltaTime, 0, 0), Quaternion.identity);
                return;
            }
            if (transform.localPosition.z > -2.25) {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(0, 0, - speed * Time.deltaTime), Quaternion.identity);
                return;
            }
            if (vip && transform.localPosition.x > -16)
            {
                transform.SetLocalPositionAndRotation(transform.localPosition + new Vector3(-speed * Time.deltaTime, 0, 0), Quaternion.identity);
                return;
            }
            if (IsAsleep())
            {
                GameState.Instance.AddPay(-spawnPrice * 2);
            }
            if (vip)
            {
                GameState.Instance.AddPay(-spawnPrice * 5 * (MealManager.EatMeal() ? -1 : 1)); // bring in 5x more money if there is a meal; remove it if there is not.
            }
            if (held.GetOldestInteractableSelected() != null)
                Destroy(held.GetOldestInteractableSelected().transform.gameObject);
            Destroy(gameObject);
            GameState.Instance.FreeSeat(target);
        }
    }

    public void SetSpawnPrice(float i)
    {
        spawnPrice = i;
    }

    public bool IsAsleep()
    {
        return transform.Find("Canvas").gameObject.activeInHierarchy;
    }

    void StartSelect(SelectEnterEventArgs arg0)
    {
        if (CoffeeTutorial.Instance != null)
        {
            GameState.Instance.menu.LoadMenu("Good job! Now try it in the real hall. Customers may fall asleep during their stay, and you must give them coffee before they leave if they do. Coffee will cost 2 dollars and you must have it ready from the cafe, so be careful!\n\nPress B to exit menu.");
            Destroy(arg0.interactableObject.transform.gameObject);
            CoffeeTutorial.Complete();
            return;
        }
        if (IsAsleep()) {
            GameObject thing = arg0.interactableObject.transform.gameObject;
            thing.GetComponent<XRGrabInteractable>().interactionLayers = InteractionLayerMask.GetMask("Coffee");
            transform.Find("Canvas").gameObject.SetActive(false);
            caffeinated = true;
        }
        else
        {
            GameState.Instance.menu.LoadMenu("People who are awake don't need coffee! You should save your resources.\n\nPress B to exit menu.");
            arg0.interactableObject.transform.position -= new Vector3(0, 0.1f, 0);
            held.interactionManager.SelectExit(held, arg0.interactableObject);
        }
        // ilm.
    }
}
