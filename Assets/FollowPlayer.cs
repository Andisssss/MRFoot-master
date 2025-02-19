using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform playerCamera;
    public float distanceFromPlayer = 2.0f;

    void Update()
    {
        if (playerCamera != null)
        {
            Vector3 newPos = playerCamera.position + playerCamera.forward * distanceFromPlayer;
            transform.position = newPos;
            transform.LookAt(playerCamera.position);
            transform.Rotate(0, 180, 0); // Flip to face the right way
        }
    }
}