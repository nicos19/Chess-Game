using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    public Slider musicSlider;
    public Toggle soundEffectsToggle;

    private void Start()
    {
        if (PlayerPrefs.HasKey("music"))
        {
            // saved music setting found -> load this setting
            musicSlider.value = PlayerPrefs.GetFloat("music");
        }

        if (PlayerPrefs.HasKey("soundEffects"))
        {
            // saved sound effects setting found -> load this setting
            if (PlayerPrefs.GetInt("soundEffects") == 0)
            {
                // sound effects shall be deactivated
                soundEffectsToggle.isOn = false;
            } else
            {
                // sound effects shall be activated
                soundEffectsToggle.isOn = true;
            }
        }

    }
}
