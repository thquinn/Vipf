using UnityEngine;
using UnityEngine.InputSystem;

public class PhantomControllerScript : MonoBehaviour {
    public static float PHANTOM_TIMESCALE = 1;

    public GameObject prefabPedestrian, prefabCar;

    public Transform transformPhantoms;
    public Vector2 zRangePedestrian, zRangePedestrianFar, zRangeCar;
    public Vector2 timeRangePedestrian, timeRangeCar;

    float timerPedestrian, timerCar;

    void Update() {
        bool speedUp = Keyboard.current.f3Key.isPressed;
        if (Time.time < 5) speedUp = !speedUp;
        PHANTOM_TIMESCALE = speedUp ? 10 : 1;
        bool beyond = PlayerScript.instance.transform.position.z > transform.position.z;
        transformPhantoms.gameObject.SetActive(!beyond);
        timerPedestrian -= Time.deltaTime * PHANTOM_TIMESCALE;
        if (timerPedestrian <= 0) {
            Vector2 range = Random.value < .5f ? zRangePedestrian : zRangePedestrianFar;
            float z = Random.Range(range.x, range.y);
            Instantiate(prefabPedestrian, transformPhantoms).GetComponent<PhantomScript>().Init(z, Random.value < .5f);
            timerPedestrian += Random.Range(timeRangePedestrian.x, timeRangePedestrian.y);
        }
        timerCar -= Time.deltaTime * PHANTOM_TIMESCALE;
        if (timerCar <= 0) {
            bool flip = Random.value < .5f;
            float z = flip ? zRangeCar.y : zRangeCar.x;
            Instantiate(prefabCar, transformPhantoms).GetComponent<PhantomScript>().Init(z, flip);
            timerCar += Random.Range(timeRangeCar.x, timeRangeCar.y);
        }
    }
}
