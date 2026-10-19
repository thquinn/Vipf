using System.Collections.Generic;
using UnityEngine;

public class Subgame04Script : MonoBehaviour {
    static int EVENT_SCORE_THRESHOLD = 20;

    public GameObject prefabPlatform, prefabFan;
    public AudioClip bgm;

    public Subgame04PlayerScript playerScript;
    public float initialDY, dyAccel;
    public Vector2 basePlatformGap, maxPlatformGap;
    public float platformGapAccel;
    public float fanGap;
    public float gameOverDelta;
    public float catchupDelta, catchupOffset, catchupTime;
    public float yPerScore;
    public Vector2 bgmLoopPoints;

    SubgameBaseScript subgame;

    float y, dy, maxPlayerY;
    float vCatchup;
    float platformGapT, nextPlatformY, nextFanY;
    List<Subgame04PlatformScript> platforms;
    List<Subgame04FanScript> fans;

    void Start() {
        subgame = GetComponentInParent<SubgameBaseScript>();
        playerScript.Init(subgame);
        platforms = new();
        fans = new();
        subgame.SetBGMLoopPoints(bgmLoopPoints);
        Restart();
    }

    void Update() {
        if (subgame.ShouldRestart()) {
            Restart();
            return;
        }
        if (subgame.isGameOver) {
            subgame.StopBGM();
            return;
        }

        dy += dyAccel * Time.deltaTime;
        y += dy * Time.deltaTime;
        subgame.cam.transform.localPosition = new Vector3(0, y, -10);
        maxPlayerY = Mathf.Max(maxPlayerY, playerScript.transform.localPosition.y);

        platformGapT = Mathf.Clamp01(platformGapT + platformGapAccel * Time.deltaTime);
        int previousScore = subgame.score;
        subgame.score = Mathf.Max(subgame.score, Mathf.CeilToInt(maxPlayerY / yPerScore));
        if (previousScore < EVENT_SCORE_THRESHOLD && subgame.score >= EVENT_SCORE_THRESHOLD) {
            subgame.EmitEvent(SubgameEvent.JumperScoreThreshold);
        }

        if (y - playerScript.transform.localPosition.y > gameOverDelta) {
            subgame.GameOver();
            return;
        }
        if (playerScript.transform.localPosition.y - y > catchupDelta) {
            float targetY = playerScript.transform.localPosition.y + catchupOffset;
            y = Mathf.SmoothDamp(y, targetY, ref vCatchup, catchupTime);
        } else {
            vCatchup = 0;
        }

        // Spawn platforms.
        while (nextPlatformY < y + 10) {
            // Note: 4 is about as high as the player can jump.
            Vector2 platformGap = Vector2.Lerp(basePlatformGap, maxPlatformGap, platformGapT);
            nextPlatformY += Random.Range(platformGap.x, platformGap.y);
            float width = 6;
            float radius = 7 - width / 4;
            float x = Random.Range(-radius, radius);
            Vector2 platformPosition = new Vector2(x, nextPlatformY);
            Subgame04PlatformScript platformScript = Instantiate(prefabPlatform, transform).GetComponent<Subgame04PlatformScript>();
            platformScript.Init(platformPosition, width);
            platforms.Add(platformScript);
        }
        while (nextFanY < y + 7) {
            if (subgame.HasXPak()) {
                Subgame04FanScript fan = Instantiate(prefabFan, transform).GetComponent<Subgame04FanScript>();
                Vector3 position = fan.transform.localPosition;
                position.y = nextFanY;
                fan.transform.localPosition = position;
                fans.Add(fan);
                subgame.Glitch();
            }
            nextFanY += fanGap;
        }
    }

    void Restart() {
        y = 0;
        subgame.cam.transform.localPosition = new Vector3(0, 0, -10);
        dy = initialDY;
        platformGapT = 0;
        maxPlayerY = 0;
        playerScript.Restart();
        nextPlatformY = -3;
        nextFanY = 5;
        foreach (var platform in platforms) if (platform != null) Destroy(platform.gameObject);
        foreach (var fan in fans) if (fan != null) Destroy(fan.gameObject);
        platforms.Clear();
        fans.Clear();
        subgame.PlayBGM(bgm);
    }
}
