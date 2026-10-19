using UnityEngine;

public class Subgame04FanScript : MonoBehaviour {
    public Sprite[] spritesFan, spritesWind;

    public SpriteRenderer srFan, srWind;
    public float force;
    public float animSpeedFan, animSpeedWind;

    void Start() {
        if (Random.value < .5) {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, 1);
        } else {
            force *= -1; // these are now black holes sucking you in, instead of fans. oops!
        }
    }

    void Update() {
        int fanSpriteIndex =  Mathf.FloorToInt(Time.time * animSpeedFan) % spritesFan.Length;
        srFan.sprite = spritesFan[fanSpriteIndex];
        int windSpriteIndex = Mathf.FloorToInt(Time.time * animSpeedWind) % spritesWind.Length;
        srWind.sprite = spritesWind[windSpriteIndex];
    }
}
