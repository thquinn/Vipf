using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Subgame04PlayerScript : MonoBehaviour {
    public Sprite spriteIdle, spriteJump, spriteSteer;
    public AudioClip sfxJump, sfxLand;

    public SpriteRenderer sr;
    public Collider2D collidre;
    public Rigidbody2D rb2d;
    public float jumpSpeed, gravity, gravityFall, horizontalInit, horizontalAccel, stoppageMult;
    public float superjumpWindow, superjumpMinSpeed, superjumpMultiplier;

    SubgameBaseScript subgame;
    Vector2 initialPosition;
    bool isGrounded;
    float lastDX, superjumpTimer;
    List<Subgame04FanScript> fans;

    public void Init(SubgameBaseScript subgame) {
        this.subgame = subgame;
        initialPosition = transform.localPosition;
        fans = new();
    }

    void Update() {
        if (subgame.isGameOver) return;
        if (isGrounded) UpdateGrounded();
        UpdateSprite();
    }
    void FixedUpdate() {
        if (!isGrounded) UpdateAir();
    }
    void UpdateGrounded() {
        if (subgame.GetKeyDown(Key.Z)) {
            isGrounded = false;
            rb2d.bodyType = RigidbodyType2D.Dynamic;
            collidre.enabled = false;
            // Check for superjump.
            float dy = jumpSpeed;
            if (superjumpTimer > 0 && Mathf.Abs(lastDX) > superjumpMinSpeed && Mathf.Sign(lastDX) == GetDX()) {
                dy += Mathf.Abs(lastDX) * superjumpMultiplier;
                // TODO: Experiment with reducing gravity.
            }
            rb2d.linearVelocity = new Vector2(horizontalInit * GetDX(), dy);
            subgame.PlaySFX(sfxJump);
        }
        superjumpTimer -= Time.deltaTime;
    }
    void UpdateAir() {
        rb2d.gravityScale = subgame.GetKey(Key.Z) ? gravity : gravityFall;
        if (rb2d.linearVelocity.y <= 0) collidre.enabled = true;
        float dx = GetDX();
        if (GetDX() != Mathf.Sign(rb2d.linearVelocity.x)) dx *= stoppageMult;
        rb2d.linearVelocity += new Vector2(dx * horizontalAccel * Time.fixedDeltaTime, 0);
        foreach (Subgame04FanScript fan in fans) {
            rb2d.linearVelocity += new Vector2(fan.force, 0) * Time.fixedDeltaTime;
        }
    }
    void UpdateSprite() {
        if (isGrounded) sr.sprite = spriteIdle;
        else if (Mathf.Abs(rb2d.linearVelocity.x) < .1f) sr.sprite = spriteJump;
        else {
            sr.sprite = spriteSteer;
            sr.flipX = rb2d.linearVelocity.x > 0;
        }
    }

    int GetDX() {
        if (subgame.GetKey(Key.LeftArrow)) {
            return -1;
        } else if (subgame.GetKey(Key.RightArrow)) {
            return 1;
        }
        return 0;
    }

    public void Restart() {
        transform.localPosition = initialPosition;
        rb2d.linearVelocity = Vector2.zero;
        rb2d.bodyType = RigidbodyType2D.Dynamic;
        collidre.enabled = true;
        isGrounded = false;
        lastDX = 0;
        superjumpTimer = 0;
        sr.sprite = spriteIdle;
        fans.Clear();
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        lastDX = rb2d.linearVelocity.x;
        superjumpTimer = superjumpWindow;
        isGrounded = true;
        rb2d.linearVelocity = new Vector2(0, 0);
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.position = new Vector2(rb2d.position.x, collision.transform.position.y + 15 / 16f);
        subgame.PlaySFX(sfxLand);
    }
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.tag == "Wind") {
            fans.Add(collision.transform.GetComponent<Subgame04FanScript>());
        }
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.tag == "Wind") {
            fans.Remove(collision.transform.GetComponent<Subgame04FanScript>());
        }
    }
}
