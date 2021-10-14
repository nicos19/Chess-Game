using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;  // represents singleton of AudioManager class

    public GameObject backgroundMusicObject;
    public GameObject moveSoundEffectObject;
    public GameObject hitSoundEffectObject;
    public GameObject selectSoundEffectObject;
    public GameObject errorSoundEffectObject;
    public GameObject winSoundEffectObject;
    public GameObject tieSoundEffectObject;
    public GameObject buttonSoundEffectObject;

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

    public void SetMusicVolume(float volume)
    {
        backgroundMusicObject.GetComponent<AudioSource>().volume = volume;
        
        PlayerPrefs.SetFloat("music", volume);
        PlayerPrefs.Save();
    }

    public void ToggleSoundEffects(bool value)
        // if "value" = false => mute all sound effects, else unmute them
    {
        moveSoundEffectObject.GetComponent<AudioSource>().mute = !value;
        hitSoundEffectObject.GetComponent<AudioSource>().mute = !value;
        selectSoundEffectObject.GetComponent<AudioSource>().mute = !value;
        errorSoundEffectObject.GetComponent<AudioSource>().mute = !value;
        winSoundEffectObject.GetComponent<AudioSource>().mute = !value;
        tieSoundEffectObject.GetComponent<AudioSource>().mute = !value;
        buttonSoundEffectObject.GetComponent<AudioSource>().mute = !value;

        if (value)
        {
            PlayerPrefs.SetInt("soundEffects", 1);
        } else
        {
            PlayerPrefs.SetInt("soundEffects", 0);
        }
        PlayerPrefs.Save();

        PlayButtonSoundEffect();
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

    public void PlayButtonSoundEffect()
    {
        buttonSoundEffectObject.GetComponent<AudioSource>().Play();
    }
}
