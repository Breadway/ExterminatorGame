using UnityEngine;

public class PlayerManager : MonoBehaviour {
    public PlayerMovement movement;
    public PlayerAim aim;
    public int moveSpeed = 20;
    void Start() {
        movement.Init(moveSpeed);
    }

    void Update() {
        
    }

    private void FixedUpdate() {
       
    }
}
