using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.IO;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject loadingPanel;
    public GameObject confirmQuitPanel;
    public GameObject settingsPanel_MainMenu;

    [Header("Main Menu Buttons")]
    public Button newGameButton;
    public Button continueButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Loading UI")]
    public Slider loadingProgressBar;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI tipText;

    [Header("Game Settings")]
    public string firstSceneName = "Town";

    private string[] loadingTips = {
        "Tip: ������ Ȱ���Ͽ� ü���� ��� ȸ���ϼ���!",
        "Tip: Shift�� ���� �޸�����!",
        "Tip: ������ �°� �ȴٸ� ��� ������ �Ǵ� �����ϼ���!",
        "Tip: ���� ���� ���� �ʹٰ��? ����Ʈ�� �ϼ���!",
        "Tip: ���� ��� ������ ������ ���� ��������!",
        "Tip: ��� ��ȭ�ϸ� ������ ������ ������!"
    };

    void Start()
    {
        InitializeMenu();
        SetupButtonEvents();
        continueButton.interactable = HasSaveData();
    }

    void InitializeMenu()
    {
        mainMenuPanel.SetActive(true);
        loadingPanel.SetActive(false);
        confirmQuitPanel.SetActive(false);
    }

    void SetupButtonEvents()
    {
        newGameButton.onClick.AddListener(StartNewGame);
        continueButton.onClick.AddListener(ContinueGame);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(ShowQuitConfirm);

        var confirmButton = confirmQuitPanel.transform.Find("ConfirmButton")?.GetComponent<Button>();
        var cancelButton = confirmQuitPanel.transform.Find("CancelButton")?.GetComponent<Button>();

        if (confirmButton != null) confirmButton.onClick.AddListener(QuitGame);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelQuit);
    }

    // ===============================
    // �� ����
    // ===============================
    public void StartNewGame()
    {
        // �ڵ����常 �ʱ�ȭ
        string autoPath = Path.Combine(Application.persistentDataPath, "save_auto.json");
        if (File.Exists(autoPath))
            File.Delete(autoPath);

        StartCoroutine(LoadSceneAndLoadGame(firstSceneName, false));
    }

    // ===============================
    // �̾��ϱ� (���� ���� ����)
    // ===============================
    public void ContinueGame()
    {
        if (HasSaveData())
            UIManager.Instance?.OpenSaveSelectUI(false);
        else
            Debug.Log("����� ������ �����ϴ�.");
    }

    // ===============================
    // ���� ���� �� �̾��ϱ� ����
    // ===============================
    public void ContinueFromSlot(int slot)
    {
        SaveLoadManager.Instance.currentSlot = slot;
        bool isAuto = (slot == 0); // 0���̸� �ڵ�����
        StartCoroutine(LoadSceneAndLoadGame(firstSceneName, true, isAuto));
    }



    // ===============================
    // �� �ε� + ���̺� ������ ����
    // ===============================
    public   IEnumerator LoadSceneAndLoadGame(string sceneName, bool loadSave, bool isAuto = false)
    {
        DontDestroyOnLoad(gameObject);

        mainMenuPanel.SetActive(false);
        loadingPanel.SetActive(true);
        Debug.Log($"[MenuManager] LoadSceneAndLoadGame ����, loadSave={loadSave}");

        if (tipText != null)
            tipText.text = loadingTips[Random.Range(0, loadingTips.Length)];

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float fakeLoadingTime = 0f;
        float totalFakeTime = 2f;

        // === ���α׷����� & �ؽ�Ʈ ���� ===
        while (!asyncLoad.isDone)
        {
            float realProgress = asyncLoad.progress;
            fakeLoadingTime += Time.deltaTime;
            float fakeProgress = fakeLoadingTime / totalFakeTime;
            float displayProgress = Mathf.Min(realProgress, fakeProgress);

            if (loadingProgressBar != null)
                loadingProgressBar.value = displayProgress;

            if (loadingText != null)
                loadingText.text = $"�ε���... {(displayProgress * 100):F0}%";

            if (realProgress >= 0.9f && fakeLoadingTime >= totalFakeTime)
                asyncLoad.allowSceneActivation = true;

            yield return null;
        }

        // === �� ��ȯ �Ϸ� �� ������ �� �� ��� ===
        yield return null;
        Debug.Log("[MenuManager] �� �ε� �Ϸ�, LoadGame ȣ�� ����");

        if (loadSave)
        {
            if (isAuto)
                SaveLoadManager.Instance.LoadAutoSave();
            else
                SaveLoadManager.Instance.LoadGame();
        }

        // === 1�� ���� �� �����ְ� ���� MenuManager �ı� ===
        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }




    // ===============================
    // ����â, ���� ��
    // ===============================
    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel_MainMenu?.SetActive(true);

        var cg = settingsPanel_MainMenu.GetComponent<CanvasGroup>();
        if (cg == null) cg = settingsPanel_MainMenu.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public void CloseSettings()
    {
        settingsPanel_MainMenu?.SetActive(false);
        mainMenuPanel.SetActive(true);

        var cg = settingsPanel_MainMenu.GetComponent<CanvasGroup>();
        if (cg == null) cg = settingsPanel_MainMenu.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public void ShowQuitConfirm() => confirmQuitPanel.SetActive(true);
    public void CancelQuit() => confirmQuitPanel.SetActive(false);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===============================
    // ���̺� ���� Ȯ��
    // ===============================
    bool HasSaveData()
    {
        for (int i = 1; i <= 3; i++)
        {
            string p = Path.Combine(Application.persistentDataPath, $"save_slot{i}.json");
            if (File.Exists(p)) return true;
        }
        return false;
    }

    // ===============================
    // ESC ó��
    // ===============================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var cg = settingsPanel_MainMenu?.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha > 0.5f)
            {
                CloseSettings();
                mainMenuPanel.SetActive(true);
                return;
            }

            if (confirmQuitPanel.activeInHierarchy)
                CancelQuit();
        }
    }
}
