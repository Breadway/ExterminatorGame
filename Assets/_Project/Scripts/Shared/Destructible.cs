using UnityEngine;

public class Destructible : MonoBehaviour {
    public void DestroyNow() {
        Destroy(gameObject);
    }
}