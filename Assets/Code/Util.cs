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

    public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Quaternion currentVelocity, float smoothTime, float maxSpeed = Mathf.Infinity, float deltaTime = -1f) {
        if (deltaTime < 0f)
            deltaTime = Time.deltaTime;

        if (Quaternion.Dot(current, target) < 0f)
            target = new Quaternion(-target.x, -target.y, -target.z, -target.w);

        Vector4 ToVector4(Quaternion q) => new Vector4(q.x, q.y, q.z, q.w);
        Quaternion FromVector4(Vector4 v) => new Quaternion(v.x, v.y, v.z, v.w);
        Quaternion Normalize(Quaternion q) {
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (mag < Mathf.Epsilon)
                return Quaternion.identity;
            return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
        }
        Vector4 currentVec = ToVector4(current);
        Vector4 targetVec = ToVector4(target);
        Vector4 velocityVec = ToVector4(currentVelocity);
        Vector4 result = new Vector4(
            Mathf.SmoothDamp(currentVec.x, targetVec.x, ref velocityVec.x, smoothTime, maxSpeed, deltaTime),
            Mathf.SmoothDamp(currentVec.y, targetVec.y, ref velocityVec.y, smoothTime, maxSpeed, deltaTime),
            Mathf.SmoothDamp(currentVec.z, targetVec.z, ref velocityVec.z, smoothTime, maxSpeed, deltaTime),
            Mathf.SmoothDamp(currentVec.w, targetVec.w, ref velocityVec.w, smoothTime, maxSpeed, deltaTime)
        );
        currentVelocity = FromVector4(velocityVec);
        return Normalize(FromVector4(result));
    }
}
