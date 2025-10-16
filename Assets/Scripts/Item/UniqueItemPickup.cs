// �÷��̾� ���� �� ���ԵǾ� �κ��丮�� ���� ������ �߰�
using UnityEngine;

public class UniqueItemPickup : MonoBehaviour, IUniquePickup
{
    public string itemName;
    public int count = 1;
    public float pickupRange = 2f;
    public float moveSpeed = 6f;
    Transform player;

    public void SetItem(string name, int amt) { itemName = name; count = amt; }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < pickupRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position + Vector3.up, moveSpeed * Time.deltaTime);
            if (dist < 0.5f)
            {
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null) inv.AddItem(itemName, count);
                Destroy(gameObject);
            }
        }
    }
}
