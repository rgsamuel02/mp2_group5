using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MealManager : MonoBehaviour
{
    static int mealsPerMinute = 1;
    static float timeToNextMeal = 60;
    public static GameObject[] meals = {null, null, null, null, null, null, null, null};
    public Button plus, minus;
    public GameObject mealPrefab;
    public TextMeshProUGUI mealTextbox;
    public KitchenMealSale kms;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        mealTextbox.text = $"{mealsPerMinute}";
    }

    void OnDisable()
    {
        
    }

    void OnEnable()
    {
        foreach (GameObject m in meals) {
            if (m != null) 
                m.SetActive(false);
        }
    }


    // Update is called once per frame
    void Update()
    {
        timeToNextMeal -= Time.deltaTime;
        if (timeToNextMeal <= 0) {
            SpawnMeal();
            timeToNextMeal = 60f / mealsPerMinute;
        }
    }

    private void SpawnMeal()
    {
        if (!kms.Buy()) return;
        GameState.Instance.AddPay(-2);
        for (int i = 0; i < meals.Length; i++)
        {
            if (meals[i] == null)
            {
                meals[i] = Instantiate(mealPrefab, gameObject.transform);
                meals[i].transform.localPosition = new Vector3(-7.5f + i,1.05000007f,1.25f);
                DontDestroyOnLoad(meals[i]);
                return;
            }
        }
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
