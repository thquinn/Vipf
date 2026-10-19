using UnityEngine;

public class PhantomScript : MonoBehaviour {
    static float SPAWN_X = 100;

    public AudioSource audioSource;
    public GameObject umbrella;

    public Vector2 speedRange;

    Vector3 velocity;

    public void Init(float z, bool flip) {
        float x = flip ? SPAWN_X : -SPAWN_X;
        transform.position = new Vector3(x, 0, z);
        transform.localRotation = flip ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        float dx = Random.Range(speedRange.x, speedRange.y) * (flip ? -1 : 1);
        velocity = new Vector3(dx, 0, 0);
        audioSource.time = Random.Range(0, audioSource.clip.length - 1);
        if (umbrella != null && Random.value < .25f) umbrella.SetActive(false);
    }

    void Update() {
        transform.localPosition += velocity * Time.deltaTime * PhantomControllerScript.PHANTOM_TIMESCALE;
        bool despawnLeft = transform.localPosition.x < -SPAWN_X && velocity.x < 0;
        bool despawnRight = transform.localPosition.x > SPAWN_X && velocity.x > 0;
        if (despawnLeft || despawnRight) {
            Destroy(gameObject);
        }
    }
}
