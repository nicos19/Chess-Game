using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;  // represents singleton of AudioManager class

    public GameObject moveSoundEffectObject;
    public GameObject hitSoundEffectObject;
    public GameObject selectSoundEffectObject;
    public GameObject errorSoundEffectObject;
    public GameObject winSoundEffectObject;
    public GameObject tieSoundEffectObject;

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

    public void PlayMoveSoundEffect()
    {
        moveSoundEffectObject.GetComponent<AudioSource>().Play();
    }

    public void PlayHitSoundEffect()
    {
        hitSoundEffectObject.GetComponent<AudioSource>().Play();
    }

    public void PlaySelectSoundEffect()
    {
        selectSoundEffectObject.GetComponent<AudioSource>().Play();
    }

    public void PlayErrorSoundEffect()
    {
        errorSoundEffectObject.GetComponent<AudioSource>().Play();
    }
    public IEnumerator PlayEndingSoundEffect(string endingSound, float delay)
    // after "delay" seconds: play given ending sound effect
    {
        yield return new WaitForSeconds(delay);

        if (endingSound == "win")
        {
            winSoundEffectObject.GetComponent<AudioSource>().Play();
        }
        else  // endingSound == "tie"
        {
            tieSoundEffectObject.GetComponent<AudioSource>().Play();
        }
    }
}
