using UnityEngine;

public class BonfireInteractable : Interactable
{
    [Header("Core References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerStats playerStats;

    [Header("Bonfire Settings")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private float enemyDetectionRadius = 15f;
    [SerializeField] private float enemyCheckInterval = 1.0f; // Check for enemies every second
    [SerializeField] private LayerMask enemyLayer;

    
    private Collider interactableCollider;

    private float enemyCheckTimer;
    private bool isSafe = true;

    private void Awake()
    {
        interactableCollider = GetComponent<Collider>();
        if (interactableCollider == null)
        {
            Debug.LogError("BonfireInteractable is missing a Collider component!", this);
            enabled = false;
            return;
        }

        if (fireParticles == null)
        {
            // Try to find it in children if not assigned
            fireParticles = GetComponentInChildren<ParticleSystem>();
            if (fireParticles == null)
                Debug.LogWarning("BonfireInteractable: No fire particle system assigned or found in children.", this);
        }

        if (enemyLayer == 0) // If layer is not set in inspector
        {
            Debug.LogError("BonfireInteractable: Enemy Layer is not set. The safety check will not work.", this);
        }
    }

    private void Start()
    {
        if (uiManager == null)
        {
            Debug.LogError("BonfireInteractable does not have a UIManager reference!");
        }
        if (playerStats == null)
        {
            Debug.LogError("BonfireInteractable does not have a PlayerStats reference!");
        }

        interactableInfoText = "Rest";
        enemyCheckTimer = enemyCheckInterval;
    }

    private void Update()
    {
        enemyCheckTimer -= Time.deltaTime;
        if (enemyCheckTimer <= 0f)
        {
            CheckForNearbyEnemies();
            enemyCheckTimer = enemyCheckInterval;
        }
    }

    private void CheckForNearbyEnemies()
    {
        bool enemyFound = Physics.CheckSphere(transform.position, enemyDetectionRadius, enemyLayer, QueryTriggerInteraction.Ignore);

        if (enemyFound)
        {
            if (isSafe)
            {
                Debug.Log("Bonfire is now unsafe due to nearby enemies.");
                SetSafeState(false);
            }
        }
        else
        {
            if (!isSafe)
            {
                Debug.Log("Bonfire is safe again.");
                SetSafeState(true);
            }
        }
    }

    private void SetSafeState(bool safe)
    {
        isSafe = safe;
        interactableCollider.enabled = safe;

        if (fireParticles != null)
        {
            if (safe)
            {
                fireParticles.Play();
            }
            else
            {
                fireParticles.Stop(true);
            }
        }

        // If the bonfire becomes unsafe while the player is in the trigger zone,
        // we need to manually remove the interaction prompt.
        if (!safe)
        {
            PlayerManager player = FindObjectOfType<PlayerManager>();
            if (player != null)
            {
                player.RemoveInteractable(this);
            }
        }
    }

    public override void OnInteract(PlayerManager playerManager)
    {
        if (!isSafe) return;

        // Restore Player Vitals
        if (playerStats != null)
        {
            playerStats.RestoreVitals();
        }

        // Respawn All Enemies
        RespawnAllEnemies();

        // Save the Game State
        SaveLoadManager.Instance.SaveGame();

        // Open the Level Up Menu
        if (uiManager != null)
        {
            Debug.Log("Interacting with bonfire. Vitals restored, enemies respawned, game saved. Opening menu.");
            uiManager.OpenLevelUpWindow();
        }
    }

    private void RespawnAllEnemies()
    {
        // Find all enemy managers in the scene and tell them to respawn.
        var allEnemies = FindObjectsOfType<EnemyManager>(true);
        foreach (var enemy in allEnemies)
        {
            enemy.RespawnEnemy();
        }
        Debug.Log("All enemies have been respawned.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyDetectionRadius);
    }
}