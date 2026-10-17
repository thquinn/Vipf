using UnityEngine;
using UnityEngine.InputSystem;

public class Subgame00Cube : MonoBehaviour {
    SubgameBaseScript subgame;

    void Start() {
        subgame = GetComponentInParent<SubgameBaseScript>();
    }

    void Update() {
        if (subgame.GetKey(Key.LeftArrow)) {
            transform.Rotate(0, Time.deltaTime * 60, 0, Space.World);
        } else if (subgame.GetKey(Key.RightArrow)) {
            transform.Rotate(0, Time.deltaTime * -60, 0, Space.World);
        }
    }
}
