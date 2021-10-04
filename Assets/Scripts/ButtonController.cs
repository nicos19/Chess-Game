using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonController : MonoBehaviour
{
    private bool creditsScreenActive;

    // Start is called before the first frame update
    void Start()
    {
        creditsScreenActive = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("escape") && creditsScreenActive)
        {
            // close credits screen if open when user hits "escape"
            CloseCredits();
        }
    }


    public void StartNewGame()
    {
        SceneManager.LoadSceneAsync("GameScene");
    }

    public void LoadGame()
    {

    }

    public void OpenSettingsMenu()
    {
        Time.timeScale = 0;
        MenuManager.Instance.gamePaused = true;
        MenuManager.Instance.settingsMenu.SetActive(true);
    }

    public void CloseSettingsMenu()
    {
        Time.timeScale = 1;
        MenuManager.Instance.gamePaused = false;
        MenuManager.Instance.settingsMenu.SetActive(false);
    }

    public void ShowCredits()
    {
        creditsScreenActive = true;
        MenuManager.Instance.creditsScreen.SetActive(true);
    }

    public void CloseCredits()
    {
        creditsScreenActive = false;
        MenuManager.Instance.creditsScreen.SetActive(false);
    }

    public void CloseGame()
    {
        Application.Quit();
    }
}
