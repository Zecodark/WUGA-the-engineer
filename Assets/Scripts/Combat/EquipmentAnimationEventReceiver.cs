using UnityEngine;

public class EquipmentAnimationEventReceiver : MonoBehaviour
{
    [SerializeField] private EquipmentSystem equipmentSystem;
    [SerializeField] private CombatInputController combatInputController;

    private void Awake()
    {
        if (equipmentSystem == null)
            equipmentSystem = GetComponentInParent<EquipmentSystem>();

        if (combatInputController == null)
            combatInputController = GetComponentInParent<CombatInputController>();
    }

    public void DrawWeapon()
    {
        if (equipmentSystem != null)
            equipmentSystem.DrawWeapon();
    }

    public void SheathWeapon()
    {
        if (equipmentSystem != null)
            equipmentSystem.SheathWeapon();

        if (combatInputController != null)
            combatInputController.FinishSheath();
    }
}
