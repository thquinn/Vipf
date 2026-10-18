using System.Linq;
using UnityEngine;

public class TapePlayerScript : MonoBehaviour {
    public static TapePlayerScript instance;

    static Color COLOR_SUPERGREEN = new Color(3, 16, 0);
    static Color COLOR_SUPERRED = new Color(16, 0, 0);

    public AudioClip finalClip;

    public MeshRenderer mrLED;
    public Transform transformLid;
    public AudioSource audioSource, audioSourceUnfiltered;
    public AudioClip sfxLoad, sfxStop, sfxEject;

    TapePlayerState state;
    float stateTimer;
    TapeScript tapeScript, previousTapeScript;
    Quaternion initialLidRotation, vLid;
    Vector3 vTape;
    Quaternion vTapeRot;
    MaterialPropertyBlock block;

    void Start() {
        instance = this;
        initialLidRotation = transformLid.localRotation;
        transformLid.localRotation = Quaternion.identity;
        block = new MaterialPropertyBlock();
    }

    void Update() {
        stateTimer -= Time.deltaTime;
        if (state == TapePlayerState.Playing) {
            float stopPoint = Mathf.Max(0, audioSource.clip.length - 2);
            float nextTime = audioSource.time + Time.deltaTime;
            if (audioSource.time < stopPoint && nextTime >= stopPoint) {
                audioSourceUnfiltered.PlayOneShot(sfxStop, 0.5f);
            }
        }
        if (state == TapePlayerState.Playing && !audioSource.isPlaying) {
            state = TapePlayerState.Stopping;
            stateTimer = 1;
            if (audioSource.clip == finalClip) {
                Invoke("Quit", 2);
            }
        }
        if (stateTimer <= 0) {
            if (state == TapePlayerState.Loading) {
                audioSource.Play();
                state = TapePlayerState.Playing;
            } else if (state == TapePlayerState.Stopping) {
                Shoot();
                state = TapePlayerState.Ejecting;
                stateTimer = 0.5f;
            } else if (state == TapePlayerState.Ejecting) {
                Release();
                state = TapePlayerState.Ready;
            }
        }
        bool lidOpen = new TapePlayerState[] { TapePlayerState.Ready, TapePlayerState.Stopping, TapePlayerState.Ejecting }.Contains(state);
        transformLid.localRotation = Util.SmoothDampQuaternion(transformLid.localRotation, lidOpen ? initialLidRotation : Quaternion.identity, ref vLid, 0.2f);
        bool holdTape = new TapePlayerState[] { TapePlayerState.Loading, TapePlayerState.Playing, TapePlayerState.Stopping }.Contains(state);
        if (holdTape) {
            tapeScript.transform.position = Vector3.SmoothDamp(tapeScript.transform.position, transform.position, ref vTape, 0.1f);
            tapeScript.transform.rotation = Util.SmoothDampQuaternion(tapeScript.transform.rotation, transform.rotation, ref vTapeRot, 0.1f);
        }
        // LED.
        mrLED.GetPropertyBlock(block);
        Color ledColor = Color.black;
        if (state == TapePlayerState.Ready) {
            ledColor = (Time.time % 1f) < 0.5f ? COLOR_SUPERGREEN : Color.black;
        }
        if (state == TapePlayerState.Playing) {
            ledColor = COLOR_SUPERRED;
        }
        block.SetColor("_EmissionColor", ledColor);
        mrLED.SetPropertyBlock(block);
    }

    private void OnTriggerEnter(Collider other) {
        if (tapeScript != null) return;
        tapeScript = other.GetComponent<TapeScript>();
        if (tapeScript == null) return;
        PlayerScript.instance.ReleaseIfHolding(tapeScript);
        Rigidbody tapeRB = tapeScript.GetComponent<Rigidbody>();
        tapeRB.isKinematic = false;
        tapeRB.linearVelocity = Vector3.zero;
        tapeRB.angularVelocity = Vector3.zero;
        tapeRB.isKinematic = true;
        Collider tapeCollider = tapeScript.GetComponent<Collider>();
        tapeCollider.enabled = false;
        audioSource.clip = tapeScript.clip;
        state = TapePlayerState.Loading;
        stateTimer = 1;
        audioSourceUnfiltered.PlayOneShot(sfxLoad, 0.5f);
    }

    void Shoot() {
        Rigidbody tapeRB = tapeScript.GetComponent<Rigidbody>();
        tapeRB.isKinematic = false;
        tapeRB.linearVelocity = 5 * Vector3.up + -1 * transform.up;
        tapeRB.angularVelocity = Random.onUnitSphere * 10;
        previousTapeScript = tapeScript;
        tapeScript = null;
        audioSourceUnfiltered.PlayOneShot(sfxEject, 0.25f);
    }
    void Release() {
        Collider tapeCollider = previousTapeScript.GetComponent<Collider>();
        tapeCollider.enabled = true;
    }

    void Quit() {
        Application.Quit();
    }
}

enum TapePlayerState {
    Ready, Loading, Playing, Stopping, Ejecting
}