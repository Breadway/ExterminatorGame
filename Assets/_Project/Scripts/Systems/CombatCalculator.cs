using UnityEngine;

public static class CombatCalculator {
    public static int CalculateDamage(int attack, int defense) {
        return Mathf.Max(0, attack - defense);
    }
}