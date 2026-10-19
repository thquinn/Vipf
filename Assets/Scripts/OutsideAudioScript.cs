using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class OutsideAudioScript : MonoBehaviour {
    public AudioMixer mixer;
    public Vector2 zRange;

    void Update() {
        float t = Mathf.InverseLerp(zRange.x, zRange.y, PlayerScript.instance.transform.position.z);
        float facadeNormalized = Mathf.Clamp(Mathf.Sqrt(1 - t), 0.0001f, 1f);
        float facadeDecibels = Mathf.Log10(facadeNormalized) * 20;
        float actualNormalized = Mathf.Clamp(Mathf.Sqrt(t), 0.0001f, 1f);
        float actualDecibels = Mathf.Log10(actualNormalized) * 20;
        mixer.SetFloat("VolumeFacade", facadeDecibels);
        mixer.SetFloat("VolumeActual", actualDecibels);
    }
}
