using System.Collections;
using UnityEngine;

public class FlashlightItem : BaseHighlightable
{
    [Header("Configurações do Item")]
    public float pickupDelay = 1.5f;
    public bool canBePickedUp = true;

    private Light myLight;

    protected override void Awake()
    {
        base.Awake();
        myLight = GetComponentInChildren<Light>();
    }

    void Start()
    {
        if (myLight != null)
        {
            myLight.enabled = true;
        }
    }

    public void OnPickup()
    {
        Destroy(gameObject);
    }
}