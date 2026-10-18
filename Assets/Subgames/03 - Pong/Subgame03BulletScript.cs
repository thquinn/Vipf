using UnityEngine;

public class Subgame03BulletScript : MonoBehaviour {
    public Rigidbody2D rb2d;

    Subgame03Script subgame03;

    public void Init(Subgame03Script subgame03) {
        this.subgame03 = subgame03;
        rb2d.position = subgame03.transform.position + subgame03.paddlePlayer.localPosition;
        rb2d.linearVelocity = new Vector2(10, 0);
    }

    private void Update() {
        if (transform.localPosition.x > 10) Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        subgame03.BulletHit(collision.transform);
        Destroy(gameObject);
    }
}
