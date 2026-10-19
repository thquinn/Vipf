using UnityEngine;
using UnityEngine.InputSystem;

public class Subgame00CubeScript : MonoBehaviour {
    SubgameBaseScript subgame;

    Quaternion initialRotation;

    void Start() {
        subgame = GetComponentInParent<SubgameBaseScript>();
        initialRotation = transform.localRotation;
    }

    void Update() {
        if (subgame.isStartBlocked) return;
        if (!subgame.isGameOver) {
            if (subgame.GetKey(Key.LeftArrow)) {
                transform.Rotate(0, Time.deltaTime * 60, 0, Space.World);
            } else if (subgame.GetKey(Key.RightArrow)) {
                transform.Rotate(0, Time.deltaTime * -60, 0, Space.World);
            }
            if (subgame.GetKeyDown(Key.Z)) {
                subgame.score++;
            }
        }
        if (subgame.GetKeyDown(Key.X)) {
            if (subgame.isGameOver) {
                transform.localRotation = initialRotation;
                subgame.isGameOver = false;
            } else {
                subgame.GameOver();
            }
        }
    }
}
