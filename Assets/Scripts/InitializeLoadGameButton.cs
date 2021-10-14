using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class InitializeLoadGameButton : MonoBehaviour
{
    public Button LoadGameButton;

    // Start is called before the first frame update
    void Start()
    {
        if (!File.Exists(Application.persistentDataPath + "/savegame.save"))
        {
            // no savegame found -> Load Game button shall not be interactable
            LoadGameButton.interactable = false; 
        }
    }
}
