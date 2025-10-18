using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Light))]
public class FlashlightBeamSensor : MonoBehaviour
{
    private Light flashlight;
    public LayerMask obstacleMask;

    public int numRays = 15;
    public int pointsPerRay = 10;
    public float raySpacing = 2.0f;

    public List<Vector3> VisibleLightPoints { get; private set; } = new List<Vector3>();

    void Awake()
    {
        flashlight = GetComponent<Light>();
    }

    void Update()
    {
        if (flashlight.enabled)
        {
            UpdateBeamPoints();
        }
        else
        {
            if (VisibleLightPoints.Count > 0)
            {
                VisibleLightPoints.Clear();
            }
        }
    }

    void UpdateBeamPoints()
    {
        VisibleLightPoints.Clear();
        float angleStep = flashlight.spotAngle / (numRays > 1 ? numRays - 1 : 1);
        float startAngle = -flashlight.spotAngle / 2f;

        for (int i = 0; i < numRays; i++)
        {
            Quaternion spreadRotation = Quaternion.AngleAxis(startAngle + (i * angleStep), transform.up);
            Quaternion tiltRotation = Quaternion.AngleAxis(Random.Range(-flashlight.spotAngle / 4, flashlight.spotAngle / 4), transform.right); // Adiciona um pouco de variação vertical
            Vector3 rayDirection = tiltRotation * spreadRotation * transform.forward;

            for (int j = 1; j <= pointsPerRay; j++)
            {
                Vector3 checkPoint = transform.position + rayDirection * (j * raySpacing);
                float distanceToCheck = Vector3.Distance(transform.position, checkPoint);

                if (distanceToCheck > flashlight.range) break;

                if (!Physics.Raycast(transform.position, rayDirection, distanceToCheck, obstacleMask))
                {
                    VisibleLightPoints.Add(checkPoint);
                }
                else
                {
                    if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, distanceToCheck, obstacleMask))
                    {
                        VisibleLightPoints.Add(hit.point);
                    }
                    break;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (flashlight != null && flashlight.enabled && VisibleLightPoints != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            foreach (Vector3 point in VisibleLightPoints)
            {
                Gizmos.DrawSphere(point, 0.1f);
                Gizmos.DrawLine(transform.position, point);
            }
        }
    }
}