using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;  // represents singleton of MenuManager class

    public bool gameRunning;
    public bool gamePaused;
    public GameObject settingsMenu;
    public GameObject creditsScreen;

    private void Awake()
    {
        if (Instance == null)
        {
            // create singleton
            Instance = this;
            gameRunning = false;
            // unity shall not destroy the attached gameObject
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // singleton already created -> destroy gameObject the new script instance (this) is attached to
            Destroy(gameObject);
        }
    }
}
