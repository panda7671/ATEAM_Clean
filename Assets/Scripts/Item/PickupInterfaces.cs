// PickupInterfaces.cs
// EnemyController, GoldPickup, UniqueItemPickup ���� �������̽�

public interface IGoldPickup
{
    void SetGold(int amount);
}

public interface IUniquePickup
{
    void SetItem(string itemName, int amount);
}
