using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public PlayerController _playerController;

    [Header("GUI")]
    public Slider healthBar;
    public Slider staminaBar;

    public int woodCount;
    public int rockCount;
    public int oreCount;

    public TextMeshProUGUI woodCountText;
    public RawImage woodCountIcon;
    public TextMeshProUGUI rockCountText;
    public RawImage rockCountIcon;
    public TextMeshProUGUI oreCountText;
    public RawImage oreCountIcon;

    // Start is called before the first frame update
    void Start()
    {
        _playerController = GameObject.Find("PlayerCharacter").GetComponent<PlayerController>();

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
        float healthValue = (float)_playerController._health / _playerController.health;
        float healthValueDifference = healthValue - healthBar.value;
        healthBar.value += healthValueDifference * 10 * Time.deltaTime;

        // Stamina Bar
        float staminaValue = _playerController._stamina / _playerController.stamina;
        float staminaValueDifference = staminaValue - staminaBar.value;
        staminaBar.value += staminaValueDifference * 10 * Time.deltaTime;
        Color staminaBarColour = _playerController._exhausted ? new(0, 0.5f + (staminaValue / 2), 0) : Color.green;
        staminaBar.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color = staminaBarColour;

        // Wood Count
        woodCountText.text = " : " + woodCount;
        if (Vector3.Distance(woodCountIcon.GetComponent<RectTransform>().localScale, Vector3.one) > 0)
            woodCountIcon.GetComponent<RectTransform>().localScale = Vector3.MoveTowards(woodCountIcon.GetComponent<RectTransform>().localScale, Vector3.one, Time.deltaTime * 10);

        // Rock Count
        rockCountText.text = " : " + rockCount;
        if (Vector3.Distance(rockCountIcon.GetComponent<RectTransform>().localScale, Vector3.one) > 0)
            rockCountIcon.GetComponent<RectTransform>().localScale = Vector3.MoveTowards(rockCountIcon.GetComponent<RectTransform>().localScale, Vector3.one, Time.deltaTime * 10);

        // Ore Count
        oreCountText.text = " : " + oreCount;
        if (Vector3.Distance(oreCountIcon.GetComponent<RectTransform>().localScale, Vector3.one) > 0)
            oreCountIcon.GetComponent<RectTransform>().localScale = Vector3.MoveTowards(oreCountIcon.GetComponent<RectTransform>().localScale, Vector3.one, Time.deltaTime * 10);
    }

    public void UpdateMaterials(MaterialInfo materialInfo)
    {
        if (materialInfo.type == MaterialInfo.Type.wood)
        {
            woodCountIcon.GetComponent<RectTransform>().localScale = Vector3.one * 2;
            woodCount += materialInfo.value;
        }
        else if (materialInfo.type == MaterialInfo.Type.rock)
        {
            rockCountIcon.GetComponent<RectTransform>().localScale = Vector3.one * 2;
            rockCount += materialInfo.value;
        }
        else if (materialInfo.type == MaterialInfo.Type.ore)
        {
            oreCountIcon.GetComponent<RectTransform>().localScale = Vector3.one * 2;
            oreCount += materialInfo.value;
        }
    }
}

[System.Serializable]
public class ElementalInfo
{
    public enum Type { neutral, earth, air, thunder, water, fire }
    public Type type;
    
    public int value;

    public ElementalInfo (Type type, int value)
    {
        this.type = type;
        this.value = value;
    }
}

[System.Serializable]
public class MaterialInfo
{
    public enum Type { wood, rock, ore }
    public Type type;

    public int value;
}
