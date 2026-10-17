using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class Util {
    public static Collider GetCameraCollider(Camera camera, LayerMask layerMask, float maxDistance = Mathf.Infinity) {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask) ? hit.collider : null;
    }

    public static void SetAlpha(this SpriteRenderer sr, float a) {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
    public static void SetAlpha(this Image image, float a) {
        Color c = image.color;
        c.a = a;
        image.color = c;
    }
    public static void SetAlpha(this TMP_Text tmp, float a) {
        Color c = tmp.color;
        c.a = a;
        tmp.color = c;
    }
}
