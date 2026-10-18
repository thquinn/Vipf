using UnityEngine;

public class Subgame04PlatformScript : MonoBehaviour {
    public SpriteRenderer sr;
    public BoxCollider2D collidre;

    public void Init(Vector2 position, float width) {
        transform.localPosition = position;
        sr.size = new Vector2(width, 1);
        collidre.size = new Vector2(width - 0.25f, collidre.size.y);
    }

    void Update() {
        
    }
}
