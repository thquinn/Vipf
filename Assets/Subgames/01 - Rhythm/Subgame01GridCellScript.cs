using UnityEngine;

public class Subgame01GridCellScript : MonoBehaviour {
    public Sprite spritePass, spriteFail;

    public SpriteRenderer srCell, srSymbol;

    Subgame01Script subgame01;
    Vector2Int coor;

    public void Init(Subgame01Script subgame01, Vector2Int coor) {
        this.subgame01 = subgame01;
        this.coor = coor;
        transform.localPosition = new Vector3(coor.x, coor.y);
    }

    void Update() {
        Subgame01GridState state = subgame01.grid[coor.x, coor.y];
        srCell.enabled = state != Subgame01GridState.Void && state != Subgame01GridState.FailVoid;
        if (state == Subgame01GridState.Pass) {
            srSymbol.sprite = spritePass;
        } else if (state == Subgame01GridState.Fail || state == Subgame01GridState.FailVoid) {
            srSymbol.sprite = spriteFail;
        } else {
            srSymbol.sprite = null;
        }
    }
}
