using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    [SerializeField] private GameObject weaponHolder;
    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject weaponSheath;

    private GameObject currentWeaponInHand;
    private GameObject currentWeaponInSheath;
    private GameObject weaponTemplate;
    private Vector3 weaponLocalPosition;
    private Quaternion weaponLocalRotation;
    private Vector3 weaponLocalScale = Vector3.one;

    private void Awake()
    {
        PrepareWeaponTemplate();
    }

    private void Start()
    {
        SetSheathedVisual();
    }

    public void DrawWeapon()
    {
        if (GetWeaponSource() == null || weaponHolder == null)
            return;

        if (currentWeaponInHand != null)
            return;

        ClearHolder(weaponHolder.transform);
        ClearHolder(weaponSheath != null ? weaponSheath.transform : null);

        currentWeaponInHand = CreateWeaponInstance(weaponHolder.transform);
        currentWeaponInSheath = null;
    }

    public void SheathWeapon()
    {
        SetSheathedVisual();
    }

    public void SetSheathedVisual()
    {
        if (GetWeaponSource() == null || weaponSheath == null)
            return;

        ClearHolder(weaponSheath.transform);
        ClearHolder(weaponHolder != null ? weaponHolder.transform : null);

        currentWeaponInSheath = CreateWeaponInstance(weaponSheath.transform);
        currentWeaponInHand = null;
    }

    private void PrepareWeaponTemplate()
    {
        if (weapon == null)
            return;

        weaponLocalPosition = weapon.transform.localPosition;
        weaponLocalRotation = weapon.transform.localRotation;
        weaponLocalScale = weapon.transform.localScale;

        if (!weapon.scene.IsValid())
            return;

        weaponTemplate = Instantiate(weapon, transform);
        weaponTemplate.name = $"{weapon.name}_Template";
        weaponTemplate.SetActive(false);
    }

    private GameObject GetWeaponSource()
    {
        return weaponTemplate != null ? weaponTemplate : weapon;
    }

    private GameObject CreateWeaponInstance(Transform holder)
    {
        GameObject source = GetWeaponSource();
        GameObject instance = Instantiate(source, holder);
        instance.name = weapon != null ? weapon.name : source.name;
        instance.transform.localPosition = weaponLocalPosition;
        instance.transform.localRotation = weaponLocalRotation;
        instance.transform.localScale = weaponLocalScale;
        instance.SetActive(true);
        return instance;
    }

    private void ClearHolder(Transform holder)
    {
        if (holder == null)
            return;

        for (int i = holder.childCount - 1; i >= 0; i--)
            Destroy(holder.GetChild(i).gameObject);
    }
}
