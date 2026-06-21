using UnityEngine;

public class PortalRotate : MonoBehaviour
{
    public Vector3 axis = new Vector3(0f, 0f, 1f);
    public float speed = 40f;

    private void Update()
    {
        transform.Rotate(axis.normalized, speed * Time.deltaTime, Space.Self);
    }
}
