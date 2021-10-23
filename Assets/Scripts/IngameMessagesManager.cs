using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngameMessagesManager : MonoBehaviour
{
    public GameObject board;
    private BoardManager boardManager;

    // Start is called before the first frame update
    void Start()
    {
        boardManager = board.GetComponent<BoardManager>();
    }

    public void DisplayIngameMessage(IEnumerator newMessage)
        // display "newMessage"
    {
        boardManager.activeIngameMessage = true;
        boardManager.activeCoroutine = newMessage;
        boardManager.newCoroutine = null;

        StartCoroutine(newMessage);
    }

    public void StartNewIngameMessage(GameObject text, float waitTime)
        // tell BoardManager that a new ERROR ingame message ("text") shall be displayed (and removed from screen after "waitTime" seconds)
    {
        AudioManager.Instance.PlayErrorSoundEffect();
        boardManager.newCoroutine = ActivateAndDeactivateMessage(text, waitTime);
    }

    private IEnumerator ActivateAndDeactivateMessage(GameObject obj, float waitTime)
        // this functions activates an object (a displayed text) and deactivates it after a delay of "waitTime" seconds
    {
        obj.SetActive(true);
        boardManager.activeText = obj;

        yield return new WaitForSeconds(waitTime);

        obj.SetActive(false);
        boardManager.activeIngameMessage = false;
        boardManager.activeCoroutine = null;
        boardManager.activeText = null;
    }
}
