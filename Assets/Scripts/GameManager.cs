using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private PlayerController playerController;

    [Header("GUI")]
    [Range(0, 1)] public float barChangeRate = 0.95f;
    public Slider healthBar;
    public Slider staminaBar;

    // Start is called before the first frame update
    void Start()
    {
        playerController = GameObject.Find("PlayerCharacter").GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGUI();
    }

    private void UpdateGUI()
    {
        // Gradualy change the value of the bars
        float healthValue = playerController._health / playerController.health;
        float healthValueDifference = healthValue - healthBar.value;
        if (Mathf.Abs(healthValueDifference) < 0.01f)
            healthBar.value = healthValue;
        else
            healthBar.value += healthValueDifference * barChangeRate * Time.deltaTime;

        float staminaValue = playerController._stamina / playerController.stamina;
        float staminaValueDifference = staminaValue - staminaBar.value;
        if (Mathf.Abs(staminaValueDifference) < 0.01f)
            staminaBar.value = staminaValue;
        else
            staminaBar.value += staminaValueDifference * barChangeRate * Time.deltaTime;
    }
}

[System.Serializable]
public class ElementalInfo
{
    public enum Type { neutral, earth, air, thunder, water, fire }
    public Type type;
    
    public int value;
}
