using System.Collections;
using UnityEngine;

public class FlashlightItem : BaseHighlightable
{
    [Header("Configurações do Item")]
    public float pickupDelay = 1.5f;
    public bool canBePickedUp = true;

    private Light myLight;

    private bool playerInRange = false;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(SettingsManager.InteractionKey))
        {
            PickUp();
        }
    }

    public void PickUp()
    {
        Destroy(gameObject);
    }
}