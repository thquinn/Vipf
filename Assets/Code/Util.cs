using UnityEngine;

public static class Util {
    public static Collider GetCameraCollider(Camera camera, LayerMask layerMask, float maxDistance = Mathf.Infinity) {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask) ? hit.collider : null;
    }
}
