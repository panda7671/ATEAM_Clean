// ����/���/��� ���̵� ���. �߰����� �ν����� ���� ����.
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyController : MonoBehaviour
{
    [Header("�߰����� ����")]
    public bool isBoss = false; // ���� ���� ����

    [Header("Stage (Region-Stage-Round)")]
    [Range(1, 5)] public int region = 1;   // ����
    [Range(1, 3)] public int stage = 1;    // ��������
    [Range(1, 3)] public int round = 1;    // ����

    [Header("Stat")]
    public int attackDamage = 5;
    public int maxHP = 5;

    [Header("�Ϲ� ���")]
    public int minGoldDrop = 5;
    public int maxGoldDrop = 10;
    public string uniqueDropName = "����� ����";
    public int minUniqueDrop = 0;
    public int maxUniqueDrop = 3;

    [Header("�߰����� ���(�ν����� ����)")]
    public int bossMinGoldDrop = 100;
    public int bossMaxGoldDrop = 200;
    public int bossMinUniqueDrop = 1;
    public int bossMaxUniqueDrop = 5;

    [Header("��� ������")]
    public GameObject goldPickupPrefab;    // GoldPickup ���� ������Ʈ
    public GameObject uniquePickupPrefab;  // UniqueItemPickup ���� ������Ʈ

    [Header("��� ����")]
    public float deathAnimHold = 1f;
    public float fadeOutDuration = 1.5f;

    [Header("����")]
    public EnemyAI ai;
    public EnemyHealth health;
    public Animator animator;

    public event Action onDeath;
    bool deadInvoked;

    void Awake()
    {
        if (!ai) ai = GetComponent<EnemyAI>();
        if (!health) health = GetComponent<EnemyHealth>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        ApplyRegionDefaults();
        if (health) health.Init(maxHP, this);

        // ��Ģ: �� ������ "3-3"�� �߰�����
        if (!isBoss && stage == 3 && round == 3) isBoss = true;
    }

    void ApplyRegionDefaults()
    {
        switch (region)
        {
            case 1:
                attackDamage = 5; maxHP = 5;
                minGoldDrop = 5; maxGoldDrop = 10;
                uniqueDropName = "����� ����"; break;
            case 2:
                attackDamage = 25; maxHP = 15;
                minGoldDrop = 10; maxGoldDrop = 25;
                uniqueDropName = "���� ����"; break;
            case 3:
                attackDamage = 30; maxHP = 50;
                minGoldDrop = 25; maxGoldDrop = 50;
                uniqueDropName = "ȭ������"; break;
            case 4:
                attackDamage = 35; maxHP = 100;
                minGoldDrop = 50; maxGoldDrop = 75;
                uniqueDropName = "��������"; break;
            case 5:
                attackDamage = 40; maxHP = 150;
                minGoldDrop = 75; maxGoldDrop = 100;
                uniqueDropName = "������ ��"; break;
        }
    }

    // �ִϸ��̼� �̺�Ʈ�κ��� ȣ���(EnemyAI.OnAttackAnimationEvent)
    public void AttackPlayer()
    {
        // ������ ���� ���� ����
        Vector3 center = transform.position + transform.forward * 1.2f + Vector3.up * 0.8f;
        float radius = 1.2f;
        int playerLayerMask = LayerMask.GetMask("Player");
        var cols = Physics.OverlapSphere(center, radius, playerLayerMask);
        foreach (var c in cols)
        {
            var ph = c.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(attackDamage);
        }
    }

    public void OnDeath()
    {
        if (deadInvoked) return;
        deadInvoked = true;
        StartCoroutine(DeathRoutine());
    }
    void DisableColliders()
    {
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    IEnumerator DeathRoutine()
    {
        if (ai) ai.enabled = false;
        DisableColliders();
        animator?.SetTrigger("Death");

        yield return new WaitForSeconds(deathAnimHold);

        int gold = isBoss
            ? UnityEngine.Random.Range(bossMinGoldDrop, bossMaxGoldDrop + 1)
            : UnityEngine.Random.Range(minGoldDrop, maxGoldDrop + 1);

        int uniqueCnt = isBoss
            ? UnityEngine.Random.Range(bossMinUniqueDrop, bossMaxUniqueDrop + 1)
            : UnityEngine.Random.Range(minUniqueDrop, maxUniqueDrop + 1);

        SpawnDrops(gold, uniqueCnt);
        yield return FadeOutAndDestroy();
        onDeath?.Invoke();
    }

    void SpawnDrops(int gold, int uniqueCnt)
    {
        Vector3 basePos = transform.position;
        if (TryGetComponent<Collider>(out var col))
            basePos.y = col.bounds.min.y + 0.2f; // �ٴڿ��� �ణ ��
        else
            basePos += Vector3.up * 0.2f;

        if (goldPickupPrefab)
        {
            var go = Instantiate(goldPickupPrefab, basePos, Quaternion.identity);
            var gp = go.GetComponent<IGoldPickup>();
            if (gp != null) gp.SetGold(gold);
        }

        if (uniquePickupPrefab && uniqueCnt > 0)
        {
            for (int i = 0; i < uniqueCnt; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0, UnityEngine.Random.Range(-0.5f, 0.5f));
                var go = Instantiate(uniquePickupPrefab, basePos + offset, Quaternion.identity);
                var up = go.GetComponent<IUniquePickup>();
                if (up != null) up.SetItem(uniqueDropName, 1);
            }
        }
    }

    IEnumerator FadeOutAndDestroy()
    {
        float t = 0f;
        var rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.material = new Material(r.material); // �ν��Ͻ�ȭ

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float a = 1f - t / fadeOutDuration;
            foreach (var r in rends)
            {
                if (r.material.HasProperty("_Color"))
                {
                    var c = r.material.color; c.a = a; r.material.color = c;
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
