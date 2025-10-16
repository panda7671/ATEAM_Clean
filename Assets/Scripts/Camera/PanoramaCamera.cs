using UnityEngine;

public class PanoramaCamera : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 10f; // �ʴ� ȸ�� �ӵ�

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}
