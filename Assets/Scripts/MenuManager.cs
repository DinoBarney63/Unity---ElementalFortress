using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Player Stuff")]
    private GameObject player;
    private PlayerController _playerController;
    private Inputs _playerInput;

    [Header("Menu Objects")]
    [SerializeField]
    private GameObject GUICanvas;
    [SerializeField]
    private GameObject startMenuCanvas;
    [SerializeField]
    private GameObject mainMenuCanvas;
    [SerializeField]
    private GameObject settingsMenuCanvas;
    [SerializeField]
    private GameObject keyboardMouseMenuCanvas;
    [SerializeField]
    private GameObject gamepadMenuCanvas;
    [SerializeField]
    private GameObject gameOverMenuCanvas;

    [Header("First Selected Buttons")]
    [SerializeField]
    private GameObject startMenuFirst;
    [SerializeField]
    private GameObject mainMenuFirst;
    [SerializeField]
    private GameObject settingsMenuFirst;
    [SerializeField]
    private GameObject keyboardMouseMenuFirst;
    [SerializeField]
    private GameObject gamepadMenuFirst;
    [SerializeField]
    private GameObject gameOverMenuFirst;

    private bool _isPaused;
    private bool _gameActive = false;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        _playerController = player.GetComponent<PlayerController>();
        _playerInput = player.GetComponent<Inputs>();

        GUICanvas.SetActive(false);
        startMenuCanvas.SetActive(true);
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);
        gameOverMenuCanvas.SetActive(false);

        _isPaused = true;
        Time.timeScale = 0f;
        _playerInput.SetCursorState(false);
        _playerController.enabled = false;
        EventSystem.current.SetSelectedGameObject(startMenuFirst);
    }

    // Update is called once per frame
    void Update()
    {
        if (_playerInput.menuOpenClose && _gameActive)
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

    private void OpenStartMenu()
    {
        startMenuCanvas.SetActive(true);
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);
        gameOverMenuCanvas.SetActive(false);

        EventSystem.current.SetSelectedGameObject(startMenuFirst);
    }

    private void OpenMainMenu()
    {
        startMenuCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);
        gameOverMenuCanvas.SetActive(false);

        EventSystem.current.SetSelectedGameObject(mainMenuFirst);
    }

    private void OpenSettingsMenu()
    {
        startMenuCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(true);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);
        gameOverMenuCanvas.SetActive(false);

        EventSystem.current.SetSelectedGameObject(settingsMenuFirst);
    }

    private void OpenKeyboardMouseMenu()
    {
        startMenuCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(true);
        gamepadMenuCanvas.SetActive(false);
        gameOverMenuCanvas.SetActive(false);

        EventSystem.current.SetSelectedGameObject(keyboardMouseMenuFirst);
    }

    private void OpenGamepadMenu()
    {
        startMenuCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(true);
        gameOverMenuCanvas.SetActive(false);

        EventSystem.current.SetSelectedGameObject(gamepadMenuFirst);
    }

    private void OpenGameOverMenu()
    {
        _gameActive = false;
        _isPaused = true;
        Time.timeScale = 0f;
        _playerInput.SetCursorState(false);
        _playerController.enabled = false;

        startMenuCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);
        gameOverMenuCanvas.SetActive(true);

        EventSystem.current.SetSelectedGameObject(gameOverMenuFirst);
    }

    private void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void CloseAllMenus()
    {
        startMenuCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        settingsMenuCanvas.SetActive(false);
        keyboardMouseMenuCanvas.SetActive(false);
        gamepadMenuCanvas.SetActive(false);
        gameOverMenuCanvas.SetActive(false);
    }

    public void OnStartPress()
    {
        _gameActive = true;
        Unpause();

        float spawnHeight = 0;
        Ray ray = new(new Vector3(0, 10, 0), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 10, 1 << 8))
        {
            spawnHeight = hit.point.y;
        }
        Vector3 spawnPosition = new(0, spawnHeight, 0);
        _playerController.MovePlayer(spawnPosition);

        GUICanvas.SetActive(true);
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
        if (_gameActive)
            OpenMainMenu();
        else
            OpenStartMenu();
    }

    public void OnKeyboardMouseBackPress()
    {
        OpenSettingsMenu();
    }

    public void OnGamepadBackPress()
    {
        OpenSettingsMenu();
    }

    public void GameOver()
    {
        OpenGameOverMenu();
    }

    public void OnBackToStartMenuPress()
    {
        ResetGame();
    }
}
