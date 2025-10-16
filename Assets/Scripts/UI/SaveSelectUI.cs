using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;

[System.Serializable]
public class SlotInfoUI
{
    public Button slotButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI itemText;
    public TextMeshProUGUI questText;
    public TextMeshProUGUI stageText;
}

public class SaveSelectUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject saveSelectPanel;      // = this.gameObject �Ǵ� �θ� �г�
    public GameObject saveSlotContainer;    // SlotContainer
    public GameObject confirmPanel;         // ConfirmPanel
    public TextMeshProUGUI confirmText;
    public Button yesButton;
    public Button noButton;
    public Button closeButton;              // ��� �ݱ�

    [Header("Slots")]
    public SlotInfoUI[] slotUIs;            // 3ĭ

    bool saveMode = false;                  // false=�ε�, true=����
    int pendingSlot = -1;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseSelf);
        gameObject.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Ȯ��â�� ���� �ִٸ� �ڷ� ���ư���
            if (confirmPanel.activeSelf)
            {
                confirmPanel.SetActive(false);
                saveSlotContainer.SetActive(true);
                return;
            }

            // �⺻: UI �ݱ�
            CloseSelf();
        }
    }

    public void Setup(bool isSaveMode)
    {
        saveMode = isSaveMode;
        pendingSlot = -1;

        saveSlotContainer.SetActive(true);
        confirmPanel.SetActive(false);
        gameObject.SetActive(true);

        // �ΰ��ӿ��� ���� �Ͻ�����
        if (saveMode) { Time.timeScale = 0f; LockPlayer(true); }

        UpdateSlotInfoDisplay();
    }

    public void CloseSelf()
    {
        // �ΰ��� ���� ��忡�� ���� ���� ����
        if (saveMode) { Time.timeScale = 1f; LockPlayer(false); }
        gameObject.SetActive(false);
    }

    void LockPlayer(bool freeze)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;
        var controller = player.GetComponent<PlayerController>();
        var anim = player.GetComponent<Animator>();
        if (controller != null) controller.canControl = !freeze;
        if (anim != null) anim.speed = freeze ? 0f : 1f;
    }

    public void OnSlotClick(int slot)
    {
        if (!saveMode)
        {
            // �ε� ���
            SaveLoadManager.Instance.currentSlot = slot;
            gameObject.SetActive(false);

            MenuManager mm = FindFirstObjectByType<MenuManager>();
            if (mm != null)
            {
                // �ڵ����� ó��
                if (slot == 0)
                {
                    SaveLoadManager.Instance.currentSlot = -1; // �ڵ����� ���п�
                    mm.StartCoroutine(mm.LoadSceneAndLoadGame(mm.firstSceneName, true, true)); // �ڵ����� ���� ȣ��
                }
                else
                {
                    mm.ContinueFromSlot(slot);
                }
            }
            return;
        }

        // ���� ���
        pendingSlot = slot;
        string path = Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");
        bool exists = File.Exists(path);

        confirmText.text = exists
            ? $"���� {slot}�� ���� �����͸� ����ðڽ��ϱ�?"
            : $"���� {slot}�� �����Ͻðڽ��ϱ�?";

        saveSlotContainer.SetActive(false);
        confirmPanel.SetActive(true);

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() => ConfirmSave(true));
        noButton.onClick.AddListener(() => ConfirmSave(false));
    }

    void ConfirmSave(bool accepted)
    {
        if (!accepted)
        {
            confirmPanel.SetActive(false);
            saveSlotContainer.SetActive(true);
            return;
        }

        SaveLoadManager.Instance.currentSlot = pendingSlot;
        SaveLoadManager.Instance.SaveGame();

        confirmText.text = "���� �Ϸ�!";
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        StartCoroutine(CloseAfterDelay());   // �״�� ȣ��
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f); // �� �ٽ�
        AfterSaved();
    }

    void AfterSaved()
    {
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
        confirmPanel.SetActive(false);
        saveSlotContainer.SetActive(true);
        UpdateSlotInfoDisplay();
    }


    void UpdateSlotInfoDisplay()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            string path;
            bool isAuto = (i == 0);

            if (isAuto)
                path = Path.Combine(Application.persistentDataPath, "save_auto.json");
            else
                path = Path.Combine(Application.persistentDataPath, $"save_slot{i}.json"); // ����1~3

            var ui = slotUIs[i];

            if (!File.Exists(path))
            {
                ui.titleText.text = isAuto ? "�ڵ����� (����)" : $"���� {i} (�� ����)";
                ui.goldText.text = "";
                ui.itemText.text = "";
                ui.questText.text = "";
                ui.stageText.text = "";
                continue;
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);

            int totalWeapons = data.player.weapons.Count;
            int totalArmors = data.player.armors.Count;
            int totalEquipments = totalWeapons + totalArmors;
            int totalQuests = data.quests.Count;
            int completedQuests = data.quests.FindAll(q => q.isCompleted).Count;
            int unlockedStages = 0;
            foreach (var r in data.regions)
                unlockedStages += r.stages.FindAll(s => s.isUnlocked).Count;

            ui.titleText.text = isAuto ? "�ڵ�����" : $"���� {i}";
            ui.goldText.text = $"���: {data.player.gold}";
            ui.itemText.text = $"���: {totalEquipments}�� (���� {totalWeapons}, �� {totalArmors})";
            ui.questText.text = $"����Ʈ: {completedQuests}/{totalQuests}";
            ui.stageText.text = $"��������: {unlockedStages}�� �ر�";
        }
    }



    public void OnResetClick(int slot)
    {
        pendingSlot = slot;
        confirmText.text = $"���� {slot}�� �ʱ�ȭ�ϰڽ��ϱ�?";
        saveSlotContainer.SetActive(false);
        confirmPanel.SetActive(true);

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() => ConfirmReset(true));
        noButton.onClick.AddListener(() => ConfirmReset(false));
    }

    void ConfirmReset(bool accepted)
    {
        if (!accepted)
        {
            confirmPanel.SetActive(false);
            saveSlotContainer.SetActive(true);
            return;
        }

        SaveLoadManager.Instance.ResetSlot(pendingSlot);
        confirmText.text = $"���� {pendingSlot}�� �ʱ�ȭ�߽��ϴ�.";

        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        Invoke(nameof(AfterReset), 2.0f);
    }

    void AfterReset()
    {
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
        confirmPanel.SetActive(false);
        saveSlotContainer.SetActive(true);
        UpdateSlotInfoDisplay();
    }
}
