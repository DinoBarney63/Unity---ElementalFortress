using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [Header("Player Stuff")]
    private GameObject player;
    private PlayerController _playerController;
    private Inputs _playerInput;

    [Header("Menu Objects")]
    [SerializeField]
    private GameObject mainMenuCanvas;
    [SerializeField]
    private GameObject settingsMenuCanvas;
    [SerializeField]
    private GameObject keyboardMouseMenuCanvas;
    [SerializeField]
    private GameObject gamepadMenuCanvas;

    [Header("First Selected Buttons")]
    [SerializeField]
    private GameObject mainMenuFirst;
    [SerializeField]
    private GameObject settingsMenuFirst;
    [SerializeField]
    private GameObject keyboardMouseMenuFirst;
    [SerializeField]
    private GameObject gamepadMenuFirst;

    private bool _isPaused;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        _playerController = player.GetComponent<PlayerController>();
        _playerInput = player.GetComponent<Inputs>();

        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (_playerInput.menuOpenClose)
        {
            if (!_isPaused)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
        }
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        _playerInput.SetCursorState(false);
        _playerController.enabled = false;
        OpenMainMenu();
    }

    public void Unpause()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        _playerInput.SetCursorState(true);
        _playerController.enabled = true;
        CloseAllMenus();
    }

    private void OpenMainMenu()
    {
        mainMenuCanvas.SetActive(true);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);

        EventSystem.current.SetSelectedGameObject(mainMenuFirst);
    }

    private void OpenSettingsMenu()
    {
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(true);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);

        EventSystem.current.SetSelectedGameObject(settingsMenuFirst);
    }

    private void OpenKeyboardMouseMenu()
    {
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(true);
        gamepadMenuCanvas.SetActive(false);

        EventSystem.current.SetSelectedGameObject(keyboardMouseMenuFirst);
    }

    private void OpenGamepadMenu()
    {
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(true);

        EventSystem.current.SetSelectedGameObject(gamepadMenuFirst);
    }

    private void CloseAllMenus()
    {
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);
    }

    public void OnSettingsPress()
    {
        OpenSettingsMenu();
    }

    public void OnResumePress()
    {
        Unpause();
    }

    public void OnSettingsKeyboardMousePress()
    {
        OpenKeyboardMouseMenu();
    }

    public void OnSettingsGamepadPress()
    {
        OpenGamepadMenu();
    }

    public void OnSettingsBackPress()
    {
        OpenMainMenu();
    }

    public void OnKeyboardMouseBackPress()
    {
        OpenSettingsMenu();
    }

    public void OnGamepadBackPress()
    {
        OpenSettingsMenu();
    }
}
