using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private PlayerController playerController;

    [Header("GUI")]
    [Range(0, 1)] public float barChangeRate = 0.99f;
    public Slider healthBar;
    public Slider staminaBar;

    public int woodCount;
    public int rockCount;
    public int oreCount;

    public TextMeshProUGUI woodCountText;
    public TextMeshProUGUI rockCountText;
    public TextMeshProUGUI oreCountText;

    // Start is called before the first frame update
    void Start()
    {
        playerController = GameObject.Find("PlayerCharacter").GetComponent<PlayerController>();

        woodCount = 0;
        rockCount = 0;
        oreCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGUI();
    }

    private void UpdateGUI()
    {
        // Gradualy change the value of the bars
        // Health Bar
        float healthValue = playerController._health / playerController.health;
        float healthValueDifference = healthValue - healthBar.value;
        if (Mathf.Abs(healthValueDifference) < 0.01f)
            healthBar.value = healthValue;
        else
            healthBar.value += healthValueDifference * barChangeRate * Time.deltaTime;

        // Stamina Bar
        float staminaValue = playerController._stamina / playerController.stamina;
        float staminaValueDifference = staminaValue - staminaBar.value;
        if (Mathf.Abs(staminaValueDifference) < 0.01f)
            staminaBar.value = staminaValue;
        else
            staminaBar.value += staminaValueDifference * barChangeRate * Time.deltaTime;
        Color staminaBarColour = playerController._exhausted ? new(0, 0.5f + (staminaValue / 2), 0) : Color.green;
        staminaBar.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color = staminaBarColour;

        // Wood Count Text
        woodCountText.text = " : " + woodCount;

        // Rock Count Text
        rockCountText.text = " : " + rockCount;

        // Ore Count Text
        oreCountText.text = " : " + oreCount;
    }
}

[System.Serializable]
public class ElementalInfo
{
    public enum Type { neutral, earth, air, thunder, water, fire }
    public Type type;
    
    public int value;
}

[System.Serializable]
public class MaterialInfo
{
    public enum Type { wood, rock, ore }
    public Type type;

    public int value;
}
