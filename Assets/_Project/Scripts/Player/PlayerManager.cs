using UnityEngine;

public class PlayerManager : MonoBehaviour {
    public PlayerMovement movement;
    public PlayerAim aim;
    public int MoveSpeed = 20;
    void Start() {
        movement.SetMoveSpeed(MoveSpeed);
    }

    void Update() {
        
    }

    private void FixedUpdate() {
       
    }
}
