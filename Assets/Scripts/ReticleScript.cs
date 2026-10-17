using UnityEngine;
using UnityEngine.UI;

public class ReticleScript : MonoBehaviour {
    public Image image;

    float vAlpha;

    void Update() {
        float targetAlpha = PlayerScript.instance.grabbedHandheld == null ? 1 : 0;
        image.SetAlpha(Mathf.SmoothDamp(image.color.a, targetAlpha, ref vAlpha, 0.05f));
    }
}
