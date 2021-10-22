using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AfterDisconnection : MonoBehaviour
{
    public GameObject loadingScreen;

    // Start is called before the first frame update
    void Start()
    {
        if (MenuManager.Instance.intentionalDisconnect)
        {
            // player intentionally disconnected -> go back to main menu
            MenuManager.Instance.intentionalDisconnect = false;
            SceneManager.LoadSceneAsync("MainMenu");
        } else
        {
            // unintentional disconnect -> show DisconnectedScreen
            loadingScreen.SetActive(false);
        }
    }
}
