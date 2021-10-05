using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleController : MonoBehaviour
{
    public void SoundEffectsToggleChanged(bool value)
        // is called when sound effects toggle recognizes a value change (toggle is set true or false)
        // activates/deactivates sound effects
    {
        AudioManager.Instance.ToggleSoundEffects(value);
    }
}
