using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonController : MonoBehaviour
{
    public GameObject board;
    public bool startMenusAssigned;  // whether "onlineStartMenu" and "offlineStartMenu" are assigned in the inspector
    public GameObject onlineStartMenu;
    public GameObject offlineStartMenu;

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
            } else if (startMenusAssigned)
            {
                if (onlineStartMenu.activeSelf)
                {
                    // close online start menu if open when user hits "escape"
                    CloseOnlineStartMenu();
                }
                else if (offlineStartMenu.activeSelf)
                {
                    // close online start menu if open when user hits "escape"
                    CloseOfflineStartMenu();
                }
            }
        }
    }

    public void OpenOnlineStartMenu()
    {
        OnlineMultiplayerActive.Instance.isOnline = true;
        onlineStartMenu.SetActive(true);
        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void CloseOnlineStartMenu()
    {
        OnlineMultiplayerActive.Instance.isOnline = false;
        onlineStartMenu.SetActive(false);
        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void OpenOfflineStartMenu()
    {
        offlineStartMenu.SetActive(true);
        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void CloseOfflineStartMenu()
    {
        offlineStartMenu.SetActive(false);
        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void StartNewGame()
    {
        // reset allMoves.txt
        File.WriteAllText(Application.persistentDataPath + "/allMoves.txt", "");
        File.WriteAllText(Application.dataPath + "/allMoves.txt", "");

        // start new game
        AudioManager.Instance.PlayButtonSoundEffect();
        MenuManager.Instance.gameRunning = true;
        SceneManager.LoadSceneAsync("GameScene");
    }

    public void LoadGame()
    {
        AudioManager.Instance.PlayButtonSoundEffect();
        MenuManager.Instance.gameRunning = true;
        MenuManager.Instance.loadGame = true;  // tells BoardManager in GameScene that a savegame shall be loaded
        SceneManager.LoadSceneAsync("GameScene");
    }

    public void BackToMainMenu()
    {
        if (MenuManager.Instance.gamePaused)
        {
            // pawn promotion menu / settings menu is open -> do not go back to main menu
            return;
        }

        // save the game
        board.GetComponent<BoardManager>().CreateSavegameFile();
        
        // back to main menu
        AudioManager.Instance.PlayButtonSoundEffect();
        MenuManager.Instance.gameRunning = false;
        SceneManager.LoadSceneAsync("MainMenu");
    }

    public void OpenSettingsMenu()
    {
        if (MenuManager.Instance.gamePaused)
        {
            // pawn promotion menu is open -> do not open settings menu
            return;
        }

        if (MenuManager.Instance.gameRunning)
        {
            Time.timeScale = 0;
            MenuManager.Instance.gamePaused = true;
        }
        MenuManager.Instance.settingsMenu.SetActive(true);
        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void CloseSettingsMenu()
    {
        if (MenuManager.Instance.gameRunning)
        {
            Time.timeScale = 1;
            MenuManager.Instance.gamePaused = false;
        }
        MenuManager.Instance.settingsMenu.SetActive(false);
        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void ShowCredits()
    {
        MenuManager.Instance.creditsScreen.SetActive(true);
        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void CloseCredits()
    {
        MenuManager.Instance.creditsScreen.SetActive(false);
        AudioManager.Instance.PlayButtonSoundEffect();
    }

    public void CloseGame()
    {
        AudioManager.Instance.PlayButtonSoundEffect();
        Application.Quit();
    }
}
