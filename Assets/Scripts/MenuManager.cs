using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;  // represents singleton of MenuManager class
    
    public GameObject settingsMenu;
    public GameObject creditsScreen;

    public bool gameRunning;  // is a chess game running currently (=> open scene is GameScene.unity)
    public bool gamePaused;  // if a game is running: is game paused (=> settings menu is opened)
    public bool loadGame;  // should a savegame be loaded (by BoardManager)?
    public bool intentionalDisconnect;  // has the player just intentionally disconnected?

    private void Awake()
    {
        if (Instance == null)
        {
            // create singleton
            Instance = this;
            // unity shall not destroy the attached gameObject
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // singleton already created -> destroy gameObject the new script instance (this) is attached to
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        gameRunning = false;
        gamePaused = false;
        loadGame = false;

        SceneManager.LoadSceneAsync("TitleScene");
    }
}
