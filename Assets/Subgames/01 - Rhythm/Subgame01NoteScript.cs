using UnityEngine;

public class Subgame01NoteScript : MonoBehaviour {
    public SpriteRenderer sr;
    public AnimationCurve opacityCurve;
    public float offset;

    float time;

    public void Init(float time) {
        this.time = time;
        Update();
    }

    void Update() {
        float deltaT = Time.time - time;
        transform.localPosition = new Vector3(0, -2.5f * deltaT + offset, 0);
        sr.SetAlpha(opacityCurve.Evaluate(deltaT));
        if (deltaT > 0 && sr.color.a <= 0) {
            Destroy(gameObject);
        }
    }
}
