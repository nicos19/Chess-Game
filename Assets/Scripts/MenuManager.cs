using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static bool gamePaused;
    public GameObject settingsMenu;

    // Start is called before the first frame update
    void Start()
    {
        gamePaused = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenSettingsMenu()
    {
        Time.timeScale = 0;
        gamePaused = true;
        settingsMenu.SetActive(true);
    }

    public void CloseSettingsMenu()
    {
        Time.timeScale = 1;
        gamePaused = false;
        settingsMenu.SetActive(false);
    }
}
