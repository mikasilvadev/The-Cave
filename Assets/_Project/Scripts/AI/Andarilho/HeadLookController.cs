using UnityEngine;

public class HeadLookController : MonoBehaviour
{
    public float lookSpeed = 5f;
    public float maxAngle = 80f;

    private Transform playerTarget;
    private Vector3 staticLookPosition;

    private enum LookMode { Default, TrackingTransform, TrackingPosition }
    private LookMode currentMode = LookMode.Default;

    private Transform bodyTransform;
    private Quaternion initialRotation;

    void Start()
    {
        bodyTransform = transform.parent;
        if (bodyTransform == null)
        {
            Debug.LogError("HeadLookController precisa estar em um objeto filho! Desativando o componente.", this);
            this.enabled = false;
            return;
        }
        initialRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        switch (currentMode)
        {
            case LookMode.TrackingTransform:
                if (playerTarget != null) TrackPosition(playerTarget.position);
                break;
            case LookMode.TrackingPosition:
                TrackPosition(staticLookPosition);
                break;
            case LookMode.Default:
                ReturnToDefault();
                break;
        }
    }

    private void TrackPosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;

        if (direction == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angle = Vector3.Angle(bodyTransform.forward, direction);

        if (angle <= maxAngle)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lookSpeed);
        }
        else
        {
            ReturnToDefault();
        }
    }

    private void ReturnToDefault()
    {
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRotation, Time.deltaTime * lookSpeed);
    }

    public void StartTracking(Transform target)
    {
        playerTarget = target;
        currentMode = LookMode.TrackingTransform;
    }

    public void LookAtPosition(Vector3 position)
    {
        staticLookPosition = position;
        currentMode = LookMode.TrackingPosition;
    }

    public void StopTracking()
    {
        currentMode = LookMode.Default;
    }
}