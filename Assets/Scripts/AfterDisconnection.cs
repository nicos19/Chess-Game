using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AfterDisconnection : MonoBehaviour
{
    public GameObject loadingScreen;
    public GameObject otherPlayerLeftGameScreen;

    // Start is called before the first frame update
    void Start()
    {
        // reset MenuManager/OnlineMultiplayerActive
        MenuManager.Instance.gameRunning = false;
        MenuManager.Instance.gamePaused = false;
        MenuManager.Instance.loadGame = false;
        OnlineMultiplayerActive.Instance.isOnline = false;

        if (MenuManager.Instance.intentionalDisconnect)
        {
            // player intentionally disconnected -> go back to main menu
            MenuManager.Instance.intentionalDisconnect = false;
            SceneManager.LoadSceneAsync("MainMenu");
        } else if (MenuManager.Instance.player2DisconnectedThroughHost || MenuManager.Instance.hostDisconnectedThroughPlayer2)
        {
            // player was disconnected when other player initiated disconnect from game
            MenuManager.Instance.player2DisconnectedThroughHost = false;
            MenuManager.Instance.hostDisconnectedThroughPlayer2 = false;
            loadingScreen.SetActive(false);
            otherPlayerLeftGameScreen.SetActive(true);  // show message that other player left the game
        }
        else
        {
            // unintentional disconnect (that was not initiated by other player) -> show DisconnectedScreen
            loadingScreen.SetActive(false);
        }
    }
}
