using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class StageAnnouncementUI : MonoBehaviour
{
    public static StageAnnouncementUI Instance;

    [Header("UI Reference")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI stageText;

    [Header("Settings")]
    public float fadeDuration = 1.0f;
    public float displayDuration = 1.5f;
    public Vector3 offset = Vector3.zero;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (stageText == null)
            stageText = GetComponentInChildren<TextMeshProUGUI>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ====== �ڵ� ǥ�� ���� ======
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        // === ���θ޴������� ǥ������ ���� ===
        if (sceneName == "MainMenu")
        {
            var cg = GetComponentInChildren<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
            stageText.text = "";
            return;
        }

        // ===== �Ϲ� �������� ǥ�� =====
        if (sceneName.StartsWith("Stage"))
        {
            // ��: Stage101_R2 �� region=1, stage=1, round=2
            // sceneName ������ �°� �Ľ�
            // StageABC_RX ���� A=����, B=��������, X=����
            string trimmed = sceneName.Replace("Stage", ""); // 101_R1
            string[] mainSplit = trimmed.Split('_');         // ["101", "R1"]

            string stageDigits = mainSplit[0];
            int regionNum = int.Parse(stageDigits.Substring(0, 1));  // 1
            int stageNum = int.Parse(stageDigits.Substring(1, 2));   // 01 �� 1

            int roundNum = 1;
            if (mainSplit.Length > 1 && mainSplit[1].StartsWith("R"))
                int.TryParse(mainSplit[1].Substring(1), out roundNum);

            // ǥ��
            ShowStageText($"�������� {stageNum} ���� {roundNum}");
            return;
        }

        // ===== Ư�� �� (Town, Shop ��) =====
        switch (sceneName)
        {
            case "Town":
                ShowStageText("����");
                break;
            case "WareHouse":
                ShowStageText("����ó");
                break;
            case "AlchemistShop":
                ShowStageText("���ݼ����� ��");
                break;
            case "EquipmentShop":
                ShowStageText("���������� ��"); // ����
                break;
            default:
                break;
        }

        //StopAllCoroutines();
    }




    // ====== ���� ǥ�� ���� ======
    public void ShowStageText(int stageNumber, int roundNumber)
    {
       
        string label = $"�������� {stageNumber}-{roundNumber}"; // ����
        StartCoroutine(ShowRoutine(label));
    }

    public void ShowStageText(string label)
    {
        StopAllCoroutines();                  // [�߰�] ���� ���̵� ��ƾ �ߴ�
        canvasGroup.alpha = 0f;               // [�߰�] ������ ���� ���¿��� ����
        StartCoroutine(ShowRoutine(label)); // ����: ��Stage �� ����
    }

    IEnumerator ShowRoutine(string label)
    {
        if (stageText == null || canvasGroup == null)
            yield break;

        stageText.text = label;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;

        // Fade In
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1;
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0;
    }
}
