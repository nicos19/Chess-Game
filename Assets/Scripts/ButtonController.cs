using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    // only set in GameScene
    public GameObject board;
    public GameObject disconnectClientWarning;

    // only set in MainMenu
    public GameObject onlineStartMenu;
    public GameObject offlineStartMenu;

    // only set in ConnectionMenu
    public InputField hostIPAddress;
    public GameObject connectingScreen;

    public bool startMenusAssigned;  // whether "onlineStartMenu" and "offlineStartMenu" are assigned in the inspector
    public bool dontSave = false;  // whether game shall be saved if BackToMainMenu() is called
    private bool disconnectClientWarningActive = false;  // whether the Disconnect Client Warning in GameScene is active currently


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


    // -----------------------------------------------------------------------
    // MENUS BUTTON CONTROLLING
    // -----------------------------------------------------------------------

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
        // ONLY FOR OFFLINE GAMES -> See ConnectForNewGame() for online games
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
    // ONLY FOR OFFLINE GAMES -> See ConnectForLoadGame() for online games
    {
        AudioManager.Instance.PlayButtonSoundEffect();
        MenuManager.Instance.gameRunning = true;
        MenuManager.Instance.loadGame = true;  // tells BoardManager in GameScene that a savegame shall be loaded
        SceneManager.LoadSceneAsync("GameScene");
    }

    public void BackToMainMenu()
    {
        if (MenuManager.Instance.gamePaused && !disconnectClientWarningActive)
        {
            // pawn promotion menu / settings menu is open -> do not go back to main menu
            return;
        }

        if (OnlineMultiplayerActive.Instance.isOnline && !disconnectClientWarning.activeSelf)
        {
            OnlineMultiplayerManager onlineMultiplayerManager = board.GetComponent<BoardManager>().onlineMultiplayerManager;
            if (onlineMultiplayerManager.isHost && onlineMultiplayerManager.joinedPlayers == 2)
            {
                // host wants to leave game while player2 is still connected -> show "Disconnect Client Warning"
                disconnectClientWarning.SetActive(true);
                disconnectClientWarningActive = true;
                AudioManager.Instance.PlayButtonSoundEffect();
                MenuManager.Instance.gamePaused = true;
                return;
            }
        }

        if (!dontSave)
        {
            // save the game
            board.GetComponent<BoardManager>().CreateSavegameFile();
        }
        
        // back to main menu
        AudioManager.Instance.PlayButtonSoundEffect();
        MenuManager.Instance.gameRunning = false;
        MenuManager.Instance.gamePaused = false;
        MenuManager.Instance.loadGame = false;
        if (OnlineMultiplayerActive.Instance.isOnline)
        {
            // for online games quit by "Back to Main Menu Button"
            OnlineMultiplayerActive.Instance.isOnline = false;
            MenuManager.Instance.intentionalDisconnect = true;
            Disconnect();
            // Network Manager then loads offline scene (DisconnectedScreen)
            // DisconnectedScreen loads immediately MainMenu (because of "intentionalDisconnect = true")
        } else
        {
            // for offline games
            SceneManager.LoadSceneAsync("MainMenu");
        }
    }

    public void CloseDisconnectClientWarning()
        // closes Disconnect Client Warning (in GameScene) -> goes back to game
    {
        disconnectClientWarning.SetActive(false);
        disconnectClientWarningActive = false;
        MenuManager.Instance.gamePaused = false;
        AudioManager.Instance.PlayButtonSoundEffect();
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


    // -----------------------------------------------------------------------
    // ONLINE BUTTON CONTROLLING
    // -----------------------------------------------------------------------

    public void ConnectForNewGame()
        // open connection menu for new online game
    {
        // reset allMoves.txt
        File.WriteAllText(Application.persistentDataPath + "/allMoves.txt", "");
        File.WriteAllText(Application.dataPath + "/allMoves.txt", "");

        // open connection menu
        AudioManager.Instance.PlayButtonSoundEffect();
        MenuManager.Instance.gameRunning = true;
        SceneManager.LoadSceneAsync("ConnectionMenu");
    }

    public void ConnectForLoadGame()
        // open connection menu to load an online game
    {
        AudioManager.Instance.PlayButtonSoundEffect();
        MenuManager.Instance.gameRunning = true;
        MenuManager.Instance.loadGame = true;  // tells BoardManager in GameScene that a savegame shall be loaded
        SceneManager.LoadSceneAsync("ConnectionMenu");
    }

    public void LeaveConnectionMenu()
        // used to go back to main menu from connection menu
        // also used to go back to main menu from DisconnectedScreen
    {
        dontSave = true;  // do not try to save since game was not started at all
        OnlineMultiplayerActive.Instance.isOnline = false;  // ensures that Disconnect() is not called
        BackToMainMenu();
    }

    public void LeaveAsyncSavegamesErrorScreen()
        // used to go back to main menu from asynSavegames error screen in GameScene
    {
        dontSave = true;  // do not try to save since game was not started at all
        BackToMainMenu();
    }

    public void StartAsHost()
    {
        AudioManager.Instance.PlayButtonSoundEffect();
        connectingScreen.SetActive(true);
        NetworkManager.singleton.StartHost();
    }

    public void JoinAsClient()
    {
        AudioManager.Instance.PlayButtonSoundEffect();
        connectingScreen.SetActive(true);
        NetworkManager.singleton.networkAddress = hostIPAddress.text;
        NetworkManager.singleton.StartClient();
    }

    public static void Disconnect()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // disconnect host
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            // disconnect client
            NetworkManager.singleton.StopClient();
        }
    }


}
