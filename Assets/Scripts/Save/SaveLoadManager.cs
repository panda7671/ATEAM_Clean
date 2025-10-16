using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;
    public int currentSlot = 1; // 1~3
    string SlotPath => Path.Combine(Application.persistentDataPath, $"save_slot{currentSlot}.json");
    string AutoPath => Path.Combine(Application.persistentDataPath, "save_auto.json");

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
        InvokeRepeating(nameof(AutoSave), 60f, 60f);
    }

    // ===========================
    // ����
    // ===========================
    SaveData CollectCurrentGameData()
    {
        Debug.Log("[Save] CollectCurrentGameData ����");

        var data = new SaveData();
        var gm = GameManager.Instance;
        var inv = FindFirstObjectByType<PlayerInventory>();
        var hp = FindFirstObjectByType<PlayerHealth>();
        var qm = QuestManager.Instance;
        var sm = StageManager.Instance;

        if (hp != null)
            data.player.currentHealth = hp.currentHealth;

        if (gm != null)
        {
            data.player.gold = gm.gold;
            Debug.Log($"[Save] gold={gm.gold}");
        }

        if (inv != null)
        {
            Debug.Log($"[Save] �κ��丮 ���� Ȯ�ε�, ����={inv.weaponStorage.Count}, ��={inv.armorStorage.Count}");

            data.player.smallPotions = inv.smallPotions;
            data.player.mediumPotions = inv.mediumPotions;
            data.player.largePotions = inv.largePotions;

            foreach (var kv in inv.items)
                data.player.items.Add(new ItemEntry { name = kv.Key, count = kv.Value });

            foreach (var w in inv.weaponStorage)
                data.player.weapons.Add(new EquipmentEntry
                {
                    itemName = w.EquipmentitemName,
                    type = (int)w.Equipmenttype,
                    statBonus = w.EquipmentstatBonus,
                    iconName = w.icon ? w.icon.name : null
                });

            foreach (var a in inv.armorStorage)
                data.player.armors.Add(new EquipmentEntry
                {
                    itemName = a.EquipmentitemName,
                    type = (int)a.Equipmenttype,
                    statBonus = a.EquipmentstatBonus,
                    iconName = a.icon ? a.icon.name : null
                });
        }
        if (inv.currentWeapon != null)
            data.player.equippedWeapon = new EquipmentEntry
            {
                itemName = inv.currentWeapon.EquipmentitemName,
                type = (int)inv.currentWeapon.Equipmenttype,
                statBonus = inv.currentWeapon.EquipmentstatBonus,
                iconName = inv.currentWeapon.icon ? inv.currentWeapon.icon.name : null
            };
        if (inv.currentArmor != null)
            data.player.equippedArmor = new EquipmentEntry
            {
                itemName = inv.currentArmor.EquipmentitemName,
                type = (int)inv.currentArmor.Equipmenttype,
                statBonus = inv.currentArmor.EquipmentstatBonus,
                iconName = inv.currentArmor.icon ? inv.currentArmor.icon.name : null
            };
        else Debug.LogWarning("[Save] PlayerInventory�� ã�� ����");

        return data;
    }

    public void SaveGame()
    {
        var data = CollectCurrentGameData();
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SlotPath, json);
        Debug.Log($"[Save] Slot{currentSlot} ���� �Ϸ� -> {SlotPath}");
    }

    public void AutoSave()
    {
        var data = CollectCurrentGameData();
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(AutoPath, json);
        Debug.Log($"[AutoSave] �Ϸ� -> {AutoPath}");
    }

    // ===========================
    // �ε�
    // ===========================
    public void LoadGame()
    {
        StartCoroutine(LoadWhenReady());
    }

    private IEnumerator LoadWhenReady()
    {
        Debug.Log($"[Load] LoadWhenReady() ����. slot={currentSlot}");

        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            FindFirstObjectByType<PlayerInventory>() != null);

        Debug.Log("[Load] GameManager & PlayerInventory �غ� �Ϸ�");

        if (!File.Exists(SlotPath))
        {
            Debug.LogWarning($"[Load] ���̺� ������ ����: {SlotPath}");
            yield break;
        }

        var json = File.ReadAllText(SlotPath);
        var data = JsonUtility.FromJson<SaveData>(json);
        Debug.Log($"[Load] JSON �б� �Ϸ� ({json.Length} bytes)");

        ApplySaveData(data);

        var inv = FindFirstObjectByType<PlayerInventory>();
        Debug.Log($"[Load] ���� ��: gold={GameManager.Instance?.gold}, " +
                  $"potions=({inv?.smallPotions},{inv?.mediumPotions},{inv?.largePotions}), " +
                  $"����={inv?.weaponStorage.Count}, ��={inv?.armorStorage.Count}");

        // UI ���ΰ�ħ
        UIManager.Instance?.UpdatePotionCount(inv.smallPotions, inv.mediumPotions, inv.largePotions);
        UIManager.Instance?.UpdateHUDPotions(inv.smallPotions, inv.mediumPotions, inv.largePotions);
        UIManager.Instance?.UpdateGoldDisplay(GameManager.Instance.gold);
        UIManager.Instance?.UpdateHUDGold(GameManager.Instance.gold);

        // â�� ����
        var warehouse = FindFirstObjectByType<WarehouseUI>();
        if (warehouse != null)
        {
            warehouse.SendMessage("RefreshAll", SendMessageOptions.DontRequireReceiver);
        }
        Debug.Log("[Load] LoadGame() �Ϸ�");
    }

    void ApplySaveData(SaveData data)
    {
        Debug.Log("[Apply] ������ ���� ����");

        var gm = GameManager.Instance;
        var inv = FindFirstObjectByType<PlayerInventory>();
        var hp = FindFirstObjectByType<PlayerHealth>();

        if (gm != null)
        {
            gm.gold = data.player.gold;
            Debug.Log($"[Apply] ��� ����: {gm.gold}");
        }
        else Debug.LogWarning("[Apply] GameManager ����");

        if (inv != null)
        {
            Debug.Log("[Apply] �κ��丮 ���� ��...");

            inv.smallPotions = data.player.smallPotions;
            inv.mediumPotions = data.player.mediumPotions;
            inv.largePotions = data.player.largePotions;

            inv.items = new Dictionary<string, int>();
            foreach (var e in data.player.items)
                inv.items[e.name] = e.count;

            inv.weaponStorage = new List<ItemEquipment>();
            foreach (var e in data.player.weapons)
                inv.weaponStorage.Add(new ItemEquipment
                {
                    EquipmentitemName = e.itemName,
                    Equipmenttype = (ShopUI.ItemType)e.type,
                    EquipmentstatBonus = e.statBonus,
                    icon = string.IsNullOrEmpty(e.iconName) ? null : Resources.Load<Sprite>($"ItemIcon/Weapon/{e.iconName}") // ��� �°�
                });

            inv.armorStorage = new List<ItemEquipment>();
            foreach (var e in data.player.armors)
                inv.armorStorage.Add(new ItemEquipment
                {
                    EquipmentitemName = e.itemName,
                    Equipmenttype = (ShopUI.ItemType)e.type,
                    EquipmentstatBonus = e.statBonus,
                    icon = string.IsNullOrEmpty(e.iconName) ? null : Resources.Load<Sprite>($"ItemIcon/Armor/{e.iconName}") // ��� �°�
                });

            Debug.Log($"[Apply] ����={inv.weaponStorage.Count}, ��={inv.armorStorage.Count}");
        }
        else Debug.LogWarning("[Apply] PlayerInventory ����");

        if (hp != null)
        {
            hp.currentHealth = Mathf.Clamp(data.player.currentHealth, 0, hp.maxHealth);
            Debug.Log($"[Apply] ü�� ����: {hp.currentHealth}/{hp.maxHealth}");
        }
        else Debug.LogWarning("[Apply] PlayerHealth ����");

        if (data.player.equippedWeapon != null && !string.IsNullOrEmpty(data.player.equippedWeapon.itemName))
        {
            var e = data.player.equippedWeapon;
            var weapon = new ItemEquipment
            {
                EquipmentitemName = e.itemName,
                Equipmenttype = (ShopUI.ItemType)e.type,
                EquipmentstatBonus = e.statBonus,
                icon = string.IsNullOrEmpty(e.iconName)
                    ? null
                    : Resources.Load<Sprite>($"ItemIcon/Weapon/{e.iconName}")
            };
            inv.EquipItem(weapon, ShopUI.ItemType.Weapon);
            FindFirstObjectByType<PlayerVisual>()?.ApplyWeapon(weapon.EquipmentitemName);
        }

        if (data.player.equippedArmor != null && !string.IsNullOrEmpty(data.player.equippedArmor.itemName))
        {
            var e = data.player.equippedArmor;
            var armor = new ItemEquipment
            {
                EquipmentitemName = e.itemName,
                Equipmenttype = (ShopUI.ItemType)e.type,
                EquipmentstatBonus = e.statBonus,
                icon = string.IsNullOrEmpty(e.iconName)
                    ? null
                    : Resources.Load<Sprite>($"ItemIcon/Armor/{e.iconName}")
            };
            inv.EquipItem(armor, ShopUI.ItemType.Armor);
            FindFirstObjectByType<PlayerVisual>()?.ApplyArmor(armor.EquipmentitemName);
        }


        Debug.Log("[Apply] ������ ���� �Ϸ�");
    }

    public void LoadAutoSave()
    {
        string autoPath = Path.Combine(Application.persistentDataPath, "save_auto.json");
        if (!File.Exists(autoPath))
        {
            Debug.LogWarning("[AutoLoad] �ڵ����� ������ �����ϴ�.");
            return;
        }

        string json = File.ReadAllText(autoPath);
        var data = JsonUtility.FromJson<SaveData>(json);
        StartCoroutine(LoadWhenReadyCustom(data));
    }

    private IEnumerator LoadWhenReadyCustom(SaveData data)
    {
        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            FindFirstObjectByType<PlayerInventory>() != null);

        ApplySaveData(data);

        var inv = FindFirstObjectByType<PlayerInventory>();
        UIManager.Instance?.UpdatePotionCount(inv.smallPotions, inv.mediumPotions, inv.largePotions);
        UIManager.Instance?.UpdateHUDPotions(inv.smallPotions, inv.mediumPotions, inv.largePotions);
        UIManager.Instance?.UpdateGoldDisplay(GameManager.Instance.gold);
        UIManager.Instance?.UpdateHUDGold(GameManager.Instance.gold);

        var warehouse = FindFirstObjectByType<WarehouseUI>();
        if (warehouse != null)
            warehouse.SendMessage("RefreshAll", SendMessageOptions.DontRequireReceiver);

        Debug.Log("[AutoLoad] �ڵ����� ������ ���� �� HUD ���� �Ϸ�");
    }



    //���̺� ���� �ʱ�ȭ �뵵
    public void ResetSlot(int slot)
    {
        string path = Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[Reset] ���̺� ���� {slot} �ʱ�ȭ �Ϸ�");
        }
    }

}
