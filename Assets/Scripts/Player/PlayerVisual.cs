using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class WeaponVisual
{
    public string weaponName;     // ��: "�Ϲ� ����", "��� ����"
    public GameObject prefab;     // ���� ������
}

[System.Serializable]
public class ArmorVisual
{
    public string armorName;      // ��: "�Ϲ� ����", "���� ����"
    public Color color;           // �� ����
}

public class PlayerVisual : MonoBehaviour
{
    [Header("Player Meshes")]
    public SkinnedMeshRenderer bodyRenderer;   // Paladin_J_Nordstrom
    public SkinnedMeshRenderer helmetRenderer; // Paladin_J_Nordstrom_Helmet

    [Header("Weapon Holder")]
    public Transform weaponHolder;             // mixamorig:RightHand
    private GameObject currentWeapon;

    [Header("Visual Tables")]
    public List<WeaponVisual> weaponVisuals = new List<WeaponVisual>();
    public List<ArmorVisual> armorVisuals = new List<ArmorVisual>();

    [Header("Defaults")]
    public GameObject defaultWeapon;
    public Color defaultArmorColor = Color.white;

    // ===== �� ���� ���� =====
    public void ApplyArmor(string armorName)
    {
        var found = armorVisuals.Find(v => v.armorName == armorName);
        Color colorToApply = found != null ? found.color : defaultArmorColor;

        ApplyColorToRenderer(bodyRenderer, colorToApply);
        ApplyColorToRenderer(helmetRenderer, colorToApply);
    }

    void ApplyColorToRenderer(SkinnedMeshRenderer renderer, Color color)
    {
        if (renderer == null) return;
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color); // URP/Lit �⺻ �÷� �Ӽ�
        renderer.SetPropertyBlock(block);
    }

    // ===== ���� �� ��ü =====
    public void ApplyWeapon(string weaponName)
    {
        if (weaponHolder == null) return;

        // ���� ���� ����
        if (currentWeapon != null)
            Destroy(currentWeapon);

        // weaponName == null �Ǵ� ��� �ȵ� ���� �� �⺻ ���� ����
        if (string.IsNullOrEmpty(weaponName))
        {
            if (defaultWeapon != null)
                defaultWeapon.SetActive(true);

            var defaultPoint = defaultWeapon?.transform.Find("AttackPoint");
            if (defaultPoint != null)
                PlayerController.Instance.attackPoint = defaultPoint;
            else
                PlayerController.Instance.attackPoint = null;
            return;
        }

        // �� ���� ���� �� �⺻ ���� ��Ȱ��ȭ
        if (defaultWeapon != null)
            defaultWeapon.SetActive(false);

        var found = weaponVisuals.Find(v => v.weaponName == weaponName);
        if (found != null && found.prefab != null)
        {
            currentWeapon = Instantiate(found.prefab, weaponHolder);

            // �� ���⿡�� attackPoint �ڵ� ����
            var newPoint = currentWeapon.GetComponentsInChildren<Transform>(true)
                            .FirstOrDefault(t => t.name == "AttackPoint");
            if (newPoint != null)
                PlayerController.Instance.attackPoint = newPoint;
            else
                PlayerController.Instance.attackPoint = null;
        }
    }
}
