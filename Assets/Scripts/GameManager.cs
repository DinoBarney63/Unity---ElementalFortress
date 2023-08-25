using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private PlayerController playerController;

    [Header("GUI")]
    public Slider healthBar;
    public Slider staminaBar;

    public int woodCount;
    public int rockCount;
    public int oreCount;

    public TextMeshProUGUI woodCountText;
    public TextMeshProUGUI rockCountText;
    public TextMeshProUGUI oreCountText;

    public GameObject testEnemy;
    public float timeTillSpawn = 30;

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

        timeTillSpawn -= Time.deltaTime;
        if (timeTillSpawn < 0)
        {
            timeTillSpawn = 30;
            GameObject newEnemy = Instantiate(testEnemy);
            newEnemy.transform.position = new Vector3(0, 5, 0);
        }
    }

    private void UpdateGUI()
    {
        // Gradualy change the value of the bars
        // Health Bar
        float healthValue = playerController._health / playerController.health;
        float healthValueDifference = healthValue - healthBar.value;
        healthBar.value += healthValueDifference * 10 * Time.deltaTime;

        // Stamina Bar
        float staminaValue = playerController._stamina / playerController.stamina;
        float staminaValueDifference = staminaValue - staminaBar.value;
        staminaBar.value += staminaValueDifference * 10 * Time.deltaTime;
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
