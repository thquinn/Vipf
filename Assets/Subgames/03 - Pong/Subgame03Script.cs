using UnityEngine;
using UnityEngine.InputSystem;

public class Subgame03Script : MonoBehaviour {
    static float CONTACT_X = 5.375f, CONTACT_Y = 5.25f;
    static float LOSE_X = 7f;
    static float MAX_EXTENT_Y = 5.5f;
    static float SPEED_PLAYER = 6;
    static float SPEED_ENEMY = 3, SPEED_ENEMY_INCREMENT = 0.3f, SPEED_ENEMY_XPAK_MULT = 1.5f;
    static float BOUNCE_DX_MULT = -1.1f;
    static float BOUNCE_DY_MULT = 3f;
    static float EXTENT_FUDGE = 0.5f;
    static float GUN_COOLDOWN = 0.66f;
    static float ENEMY_STUN_TIME = 2f;

    public GameObject prefabBullet;
    public AudioClip sfxBouncePlayer, sfxBounceEnemy, sfxBounceWall, sfxPoint, sfxShoot, sfxHitBall, sfxHitEnemy;

    public Transform ball, paddlePlayer, paddleEnemy, gun;

    SubgameBaseScript subgame;
    Vector3 vBall;
    float gunTimer;
    float enemyStunTimer;

    void Start() {
        subgame = GetComponentInParent<SubgameBaseScript>();
        Restart();
    }

    void Update() {
        if (subgame.ShouldRestart()) {
            Restart();
            return;
        }
        if (subgame.isGameOver) return;

        // Special event.
        if (subgame.GetKey(Key.Z) && subgame.GetKey(Key.X)) {
            subgame.EmitEvent(SubgameEvent.PongZX);
        }

        // Player paddle.
        int dy = 0;
        if (subgame.GetKey(Key.UpArrow)) dy++;
        if (subgame.GetKey(Key.DownArrow)) dy--;
        paddlePlayer.localPosition += new Vector3(0, dy * Time.deltaTime * SPEED_PLAYER, 0);
        ClampPaddlePosition(paddlePlayer);

        // Player shooting.
        gun.gameObject.SetActive(subgame.HasXPak());
        gun.localPosition = paddlePlayer.localPosition;
        gunTimer -= Time.deltaTime;
        if (gunTimer <= 0 && subgame.GetKey(Key.Z) && subgame.HasXPak()) {
            Instantiate(prefabBullet, transform).GetComponent<Subgame03BulletScript>().Init(this);
            gunTimer = GUN_COOLDOWN;
            subgame.PlaySFX(sfxShoot);
        }

        // Enemy paddle.
        enemyStunTimer -= Time.deltaTime;
        if (enemyStunTimer <= 0) {
            float enemyToBall = ball.localPosition.y - paddleEnemy.localPosition.y;
            float enemySpeed = SPEED_ENEMY + subgame.score * SPEED_ENEMY_INCREMENT;
            if (subgame.HasXPak()) enemySpeed *= SPEED_ENEMY_XPAK_MULT;
            float travel = Mathf.Min(Mathf.Abs(enemyToBall), Time.deltaTime * enemySpeed);
            travel *= Mathf.Sign(enemyToBall);
            paddleEnemy.localPosition += new Vector3(0, travel, 0);
            ClampPaddlePosition(paddleEnemy);
        }

        Vector3 nextBallPosition = ball.localPosition + vBall * Time.deltaTime;
        // Player paddle check.
        if (ball.localPosition.x > -CONTACT_X && nextBallPosition.x <= -CONTACT_X) {
            if (IsWithinExtents(ball.localPosition.y, paddlePlayer)) {
                PerformBounce(paddlePlayer);
                subgame.PlaySFX(sfxBouncePlayer);
            }
        }
        if (ball.localPosition.x < -LOSE_X) {
            subgame.GameOver();
        }
        // Enemy paddle check.
        if (ball.localPosition.x < CONTACT_X && nextBallPosition.x >= CONTACT_X) {
            if (IsWithinExtents(ball.localPosition.y, paddleEnemy)) {
                PerformBounce(paddleEnemy);
                subgame.PlaySFX(sfxBounceEnemy);
            }
        }
        if (ball.localPosition.x > LOSE_X) {
            subgame.AddPoints(1);
            ResetBall(-1);
            subgame.PlaySFX(sfxPoint);
        }
        // Move ball.
        ball.localPosition = ball.localPosition + vBall * Time.deltaTime;
        if (ball.localPosition.y <= -CONTACT_Y && vBall.y < 0) {
            vBall.y *= -1;
            subgame.PlaySFX(sfxBounceWall);
        } else if (ball.localPosition.y >= CONTACT_Y && vBall.y > 0) {
            vBall.y *= -1;
            subgame.PlaySFX(sfxBounceWall);
        }
    }

    void Restart() {
        paddlePlayer.localPosition = new Vector3(paddlePlayer.localPosition.x, 0, 0);
        paddleEnemy.localPosition = new Vector3(paddleEnemy.localPosition.x, 0, 0);
        ResetBall(1);
    }
    void ResetBall(int direction) {
        ball.localPosition = Vector3.zero;
        vBall = new Vector3(3 * direction, 1);
    }

    void ClampPaddlePosition(Transform paddle) {
        Vector2 yExtents = GetYExtents(paddle);
        if (yExtents.x < -MAX_EXTENT_Y) {
            paddle.localPosition += new Vector3(0, -MAX_EXTENT_Y - yExtents.x, 0);
        }
        if (yExtents.y > MAX_EXTENT_Y) {
            paddle.localPosition -= new Vector3(0, yExtents.y - MAX_EXTENT_Y, 0);
        }
    }
    bool IsWithinExtents(float y, Transform paddle) {
        Vector2 yExtents = GetYExtents(paddle);
        return y >= yExtents.x && y <= yExtents.y;
    }
    Vector2 GetYExtents(Transform paddle) {
        float radius = paddle.localScale.y;
        return new Vector2(paddle.localPosition.y - radius / 2 - EXTENT_FUDGE, paddle.localPosition.y + radius / 2 + EXTENT_FUDGE);
    }

    void PerformBounce(Transform paddle) {
        vBall.x *= BOUNCE_DX_MULT;
        Vector2 yExtents = GetYExtents(paddle);
        float normalizedContact = Mathf.InverseLerp(yExtents.x, yExtents.y, ball.localPosition.y) * 2 - 1;
        vBall.y += normalizedContact * BOUNCE_DY_MULT;
    }

    public void BulletHit(Transform target) {
        if (target == ball) {
            vBall.x += 0.2f;
            subgame.PlaySFX(sfxHitBall);
        }
        if (target == paddleEnemy) {
            enemyStunTimer = ENEMY_STUN_TIME;
            subgame.PlaySFX(sfxHitEnemy);
        }
    }
}
