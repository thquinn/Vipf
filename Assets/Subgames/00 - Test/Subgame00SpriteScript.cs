using UnityEngine;

public class Subgame00SpriteScript : MonoBehaviour {
    public Sprite[] sprites;

    public SpriteRenderer sr;
    
    void Update() {
        int index = Mathf.FloorToInt(Time.time % sprites.Length);
        sr.sprite = sprites[index];
    }
}
