using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private PlayerController playerController;
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
        healthBar.value = playerController._health / playerController.health;
        staminaBar.value = playerController._stamina / playerController.stamina;
    }
}

[System.Serializable]
public class ElementalInfo
{
    public enum Type { neutral, earth, air, thunder, water, fire }
    public Type type;
    
    public int value;
}
