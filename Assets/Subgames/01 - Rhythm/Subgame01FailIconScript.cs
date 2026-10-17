using UnityEngine;

public class Subgame01FailIconScript : MonoBehaviour {
    public Sprite spriteLife, spriteLifeLost;

    public SpriteRenderer sr;

    Subgame01Script subgame01;
    int index;

    public void Init(Subgame01Script subgame01, int index) {
        this.subgame01 = subgame01;
        this.index = index;
        transform.localPosition = new Vector3(index * 1.5f, 0, 0);
    }

    void Update() {
        if (index >= subgame01.fails.y) {
            sr.sprite = null;
        } else {
            sr.sprite = subgame01.fails.x < index + 1 ? spriteLife : spriteLifeLost;
        }
    }
}
