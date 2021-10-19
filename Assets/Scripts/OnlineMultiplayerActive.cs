using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnlineMultiplayerActive : MonoBehaviour
{
    public static OnlineMultiplayerActive Instance;  // represents singleton of OnlineMultiplayerActive class

    public bool isOnline;

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
}
