using UnityEngine;

public class PlayerManager : MonoBehaviour {
    public PlayerMovement movement;
    public PlayerAim aim;
    void Start() {
        
    }

    void Update() {
        
    }

    private void FixedUpdate() {
        movement.CalcMovement();
        
    }
}
