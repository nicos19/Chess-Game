using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonController : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("escape"))
        {
            if (MenuManager.Instance.creditsScreen.activeSelf)
            {
                // close credits screen if open when user hits "escape"
                CloseCredits();
            } else if (MenuManager.Instance.settingsMenu.activeSelf)
            {
                // close settings menu if open when user hits "escape"
                CloseSettingsMenu();
            }
        }
    }

    public void StartNewGame()
    {
        MenuManager.Instance.gameRunning = true;
        SceneManager.LoadSceneAsync("GameScene");
    }

    public void LoadGame()
    {
        MenuManager.Instance.gameRunning = true;

    }

    public void BackToMainMenu()
    {
        MenuManager.Instance.gameRunning = false;
        SceneManager.LoadSceneAsync("MainMenu");
    }

    public void OpenSettingsMenu()
    {
        if (MenuManager.Instance.gameRunning)
        {
            Time.timeScale = 0;
            MenuManager.Instance.gamePaused = true;
        }
        MenuManager.Instance.settingsMenu.SetActive(true);
    }

    public void CloseSettingsMenu()
    {
        if (MenuManager.Instance.gameRunning)
        {
            Time.timeScale = 1;
            MenuManager.Instance.gamePaused = false;
        }
        MenuManager.Instance.settingsMenu.SetActive(false);
    }

    public void ShowCredits()
    {
        MenuManager.Instance.creditsScreen.SetActive(true);
    }

    public void CloseCredits()
    {
        MenuManager.Instance.creditsScreen.SetActive(false);
    }

    public void CloseGame()
    {
        Application.Quit();
    }
}
