using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

public class MealTutorial : MonoBehaviour
{
    int mealsPerMinute = 6;
    float timeToNextMeal = 10;
    static GameObject[] meals = {null, null, null, null, null};
    public Button plus, minus;
    public GameObject mealPrefab;
    public TextMeshProUGUI mealTextbox;
    public XROrigin xro;
    bool firstMenuClosed = false;
    static bool tutorialCleared = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (tutorialCleared)
        {
            Destroy(gameObject);
        }
        plus.onClick.AddListener(() => {
            mealsPerMinute++;
            mealTextbox.text = $"{mealsPerMinute}";
            minus.interactable = true;
            });
        minus.onClick.AddListener(() => {
            mealsPerMinute--;
            mealTextbox.text = $"{mealsPerMinute}";
            if (mealsPerMinute == 1) minus.interactable = false;
            });
            GameState.Instance.menu.LoadMenu("You've started to get some VIP guests. This is great news; they have lots of money to donate, but they also want meals after their concerts! You've partnered with Hell's Kitchen to get them meals.\n\nThere's an issue though. While they will donate if they get meals as they leave, if there aren't any meals, they take away money! Even worse, Hell's Kitchen can only start new dishes after finishing the current ones. While you can adjust the frequency of meals arriving, it only takes effect after the next meal arrives.\n\nPress B to continue.");
    }

    // Update is called once per frame
    void Update()
    {
        if (!firstMenuClosed && !GameState.Instance.menu.IsOpen())
        {
            GameState.Instance.menu.LoadMenu("Try experimenting with the system to get 5 dishes before real VIPs start coming.\n\nPress B to close");
            firstMenuClosed = true;
        }
        timeToNextMeal -= Time.deltaTime;
        if (timeToNextMeal <= 0) {
            SpawnMeal();
            timeToNextMeal = 60f / mealsPerMinute;
        }
    }

    private void SpawnMeal()
    {
        for (int i = 0; i < meals.Length; i++)
        {
            if (meals[i] == null)
            {
                meals[i] = Instantiate(mealPrefab, gameObject.transform);
                meals[i].transform.localPosition = new Vector3(-2.2f + 0.3f*i,1.05000007f,2);
                return;
            }
        }
        tutorialCleared = true;
        Destroy(gameObject);
        xro.transform.position = new(0, 0, 0);
        GameState.Instance.menu.LoadMenu("You're an expert. Now let's try it for real. You can see the restaurant section on your left, but meals cost money now. 2 dollars in fact. Worse, if you make too many meals, you have to throw them away, but you are still paying! You will have to find the balance. Good luck!\n\nPress B to continue.");
    }

    public static bool EatMeal()
    {
        for (int i = 0; i < meals.Length; i++)
        {
            if (meals[i] != null)
            {
                Destroy(meals[i]);
                meals[i] = null;
                return true;
            }
        }
        return false;
    }
}
