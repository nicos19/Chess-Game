using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
    public Text valueText;


    // Start is called before the first frame update
    void Start()
    {
        valueText.text = ((int)Mathf.Floor(gameObject.GetComponent<Slider>().value * 100)).ToString();
    }

    public void MusicSliderChanged(float value)
        // is called when music slider recognizes a value change: sets music volume to "value"
    {
        valueText.text = ((int)Mathf.Floor(value * 100)).ToString();
        AudioManager.Instance.SetMusicVolume(value);
    }

}
