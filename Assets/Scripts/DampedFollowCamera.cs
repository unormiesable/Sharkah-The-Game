using UnityEngine;

public class DampedFollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float fixedXPosition = 30f; 
    public float followHeight = 3f;
    public float followDistance = -10f;

    [Header("Damping")]
    public float positionDampSpeed = 5f;
    public float rotationDampSpeed = 3f;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Target Not Set");
            return;
        }

        Vector3 desiredPosition = new Vector3(
            fixedXPosition,
            target.position.y + followHeight,
            target.position.z + followDistance
        );

        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            positionDampSpeed * Time.deltaTime
        );
        
        smoothedPosition.x = fixedXPosition; 
        transform.position = smoothedPosition;

        
        
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationDampSpeed * Time.deltaTime
        );
    }
}