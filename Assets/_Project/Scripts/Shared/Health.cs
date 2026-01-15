using UnityEngine;

public class Health : MonoBehaviour {
    public int MaxHP = 100;
    public int CurrentHP;

    void Awake() {
        CurrentHP = MaxHP;
    }

    public void TakeDamage(int amount) {
        CurrentHP -= amount;
        if (CurrentHP <= 0) Destroy(gameObject);
    }
}