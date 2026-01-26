using UnityEngine;

public class ActivateRotate : MonoBehaviour
{
    [SerializeField]
    bool activate = false;

    [SerializeField]
    float rotationSpeed = 90f;

    void Update()
    {
        if (!activate)
        {
            return;
        }

        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }
}
