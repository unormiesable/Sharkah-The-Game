using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PopulationManager : MonoBehaviour 
{
    [Header("Population Settings")]
    public PreyController fishPrefab; 
    public int initialPopulation = 30;
    public Vector3 spawnBounds = new Vector3(1f, 10f, 10f); 
    
    public float minScaleIncrease = 0.0f;
    public float maxScaleIncrease = 0.5f;

    [Header("Genetic Algorithm")]
    public float generationTime = 45f; 
    private float generationTimer;
    [Range(0, 1)]
    public float mutationChance = 0.05f;
    [Range(0, 1)]
    public float mutationAmount = 0.1f;
    
    private int currentGeneration = 1;
    private bool isExtinct = false;

    [Header("Distance Culling (Logic LOD)")]
    public float activationDistance = 50f; 
    
    private bool isPopulationActive = true;

    [Header("Wander Target")]
    public Transform wanderTarget;
    public float wanderTargetSpeed = 1f;
    public float wanderTargetBounds = 10f;
    
    private Vector3 initialWanderPosition;

    [Header("Shark Avoidance")]
    [Tooltip("Seberapa jauh wander target akan kabur dari hiu.")]
    public float sharkAvoidanceRadius = 25f;
    public float fleeSpeed = 3f;
    
    private Transform sharkTransform;

    [HideInInspector]
    public List<PreyController> allFish = new List<PreyController>();
    
    private float fixedXPosition;

    void Start()
    {
        fixedXPosition = transform.position.x;

        if (wanderTarget != null)
        {
            initialWanderPosition = wanderTarget.position;
            initialWanderPosition.x = fixedXPosition; 
            wanderTarget.position = initialWanderPosition;
        }

        SharkController shark = FindFirstObjectByType<SharkController>();
        if (shark != null)
        {
            sharkTransform = shark.transform;
        } 
        else
        {
            Debug.LogWarning("PopulationManager: Tidak bisa menemukan SharkController di scene!");
        }

        SpawnInitialPopulation();
        generationTimer = generationTime;
        Debug.Log($"Generasi 1 Dimulai untuk {fishPrefab.name} di {gameObject.name}");
    }

    void Update()
    {
        if (isExtinct) return;

        if (sharkTransform != null)
        {
            float distanceToShark = Vector3.Distance(transform.position, sharkTransform.position);

            if (distanceToShark > activationDistance && isPopulationActive)
            {
                DeactivatePopulation();
            }
            else if (distanceToShark <= activationDistance && !isPopulationActive)
            {
                ActivatePopulation();
            }
        }

        generationTimer -= Time.deltaTime;
        if (generationTimer <= 0)
        {
            StartNextGeneration();
            generationTimer = generationTime; 
        }

        if (isPopulationActive)
        {
            UpdateWanderTarget();
        }
    }


    void ActivatePopulation()
    {
        isPopulationActive = true;
        foreach (PreyController fish in allFish)
        {
            if (fish != null)
            {
                fish.enabled = true;
            }
        }
    }

    void DeactivatePopulation()
    {
        isPopulationActive = false;
        foreach (PreyController fish in allFish)
        {
            if (fish != null)
            {
                fish.enabled = false; 
            }
        }
    }


    void SpawnInitialPopulation()
    {
        for (int i = 0; i < initialPopulation; i++)
        {
            Vector3 randomPos = GetRandomSpawnPos();
            PreyController fish = Instantiate(fishPrefab, randomPos, Quaternion.identity);
            
            InitializeFish(fish);
            Mutate(fish);
        }
    }


    void StartNextGeneration()
    {
        currentGeneration++;
        
        List<PreyController> survivors = allFish.Where(fish => fish != null).ToList();

        if (survivors.Count == 0)
        {
            Debug.LogWarning($"Semua ikan {fishPrefab.name} di {gameObject.name} telah punah!");
            isExtinct = true;
            return;
        }
        
        Debug.Log($"Generasi {currentGeneration} ({fishPrefab.name}) Dimulai! Survivors: {survivors.Count}");

        int fishToCreate = initialPopulation - survivors.Count;

        for (int i = 0; i < fishToCreate; i++)
        {
            PreyController parent1 = survivors[Random.Range(0, survivors.Count)];
            PreyController parent2 = survivors[Random.Range(0, survivors.Count)];
            
            Reproduce(parent1, parent2);
        }
    }


    void Reproduce(PreyController p1, PreyController p2)
    {
        Vector3 spawnPos = GetRandomSpawnPos();
        PreyController child = Instantiate(fishPrefab, spawnPos, Quaternion.identity);

        child.maxMoveSpeed = (Random.value < 0.5f) ? p1.maxMoveSpeed : p2.maxMoveSpeed;
        child.perceptionRadius = (Random.value < 0.5f) ? p1.perceptionRadius : p2.perceptionRadius;
        child.avoidanceRadius = (Random.value < 0.5f) ? p1.avoidanceRadius : p2.avoidanceRadius;
        child.dangerRadius = (Random.value < 0.5f) ? p1.dangerRadius : p2.dangerRadius;
        
        child.alignmentWeight = (Random.value < 0.5f) ? p1.alignmentWeight : p2.alignmentWeight;
        child.cohesionWeight = (Random.value < 0.5f) ? p1.cohesionWeight : p2.cohesionWeight;
        child.separationWeight = (Random.value < 0.5f) ? p1.separationWeight : p2.separationWeight;
        child.wanderWeight = (Random.value < 0.5f) ? p1.wanderWeight : p2.wanderWeight;
        child.fleeWeight = (Random.value < 0.5f) ? p1.fleeWeight : p2.fleeWeight;
        
        Mutate(child);
        InitializeFish(child);
    }


    void Mutate(PreyController fish)
    {
        if (Random.value < mutationChance)
            fish.maxMoveSpeed *= (1 + Random.Range(-mutationAmount, mutationAmount));
        if (Random.value < mutationChance)
            fish.perceptionRadius *= (1 + Random.Range(-mutationAmount, mutationAmount));
        if (Random.value < mutationChance)
            fish.avoidanceRadius *= (1 + Random.Range(-mutationAmount, mutationAmount));
        if (Random.value < mutationChance)
            fish.dangerRadius *= (1 + Random.Range(-mutationAmount, mutationAmount));
        if (Random.value < mutationChance)
            fish.alignmentWeight *= (1 + Random.Range(-mutationAmount, mutationAmount));
        if (Random.value < mutationChance)
            fish.cohesionWeight *= (1 + Random.Range(-mutationAmount, mutationAmount));
        if (Random.value < mutationChance)
            fish.separationWeight *= (1 + Random.Range(-mutationAmount, mutationAmount));
        if (Random.value < mutationChance)
            fish.wanderWeight *= (1 + Random.Range(-mutationAmount, mutationAmount));
        if (Random.value < mutationChance)
            fish.fleeWeight *= (1 + Random.Range(-mutationAmount, mutationAmount));

        fish.maxMoveSpeed = Mathf.Max(0.1f, fish.maxMoveSpeed);
        fish.perceptionRadius = Mathf.Max(0.1f, fish.perceptionRadius);
    }


    void InitializeFish(PreyController fish)
    {
        Vector3 baseScale = fish.transform.localScale;
        float randomAddition = Random.Range(minScaleIncrease, maxScaleIncrease);
        fish.transform.localScale = baseScale + Vector3.one * randomAddition;
        
        fish.manager = this;
        fish.xCenterPosition = this.fixedXPosition; 
        fish.xMoveRange = this.spawnBounds.x;
        
        allFish.Add(fish);

        fish.enabled = isPopulationActive;
    }


    Vector3 GetRandomSpawnPos()
    {
        return new Vector3(
            fixedXPosition + Random.Range(-spawnBounds.x, spawnBounds.x),
            transform.position.y + Random.Range(-spawnBounds.y, spawnBounds.y),
            transform.position.z + Random.Range(-spawnBounds.z, spawnBounds.z)
        );
    }


    void UpdateWanderTarget()
    {
        if (wanderTarget == null) return;

        Vector3 nextPos;
        
        if (sharkTransform != null && Vector3.Distance(sharkTransform.position, wanderTarget.position) < sharkAvoidanceRadius)
        {
            Vector3 fleeDirection = (wanderTarget.position - sharkTransform.position).normalized;
            nextPos = wanderTarget.position + fleeDirection * fleeSpeed * Time.deltaTime;
        }
        else
        {
            nextPos = Vector3.MoveTowards(wanderTarget.position, initialWanderPosition, wanderTargetSpeed * Time.deltaTime);
        }

        nextPos.x = Mathf.Clamp(nextPos.x, fixedXPosition - spawnBounds.x, fixedXPosition + spawnBounds.x);
        
        if (Vector3.Distance(initialWanderPosition, nextPos) > wanderTargetBounds)
        {
            nextPos = initialWanderPosition + (nextPos - initialWanderPosition).normalized * wanderTargetBounds;
        }
        
        wanderTarget.position = nextPos;
    }


    public void RemoveFish(PreyController fish)
    {
        if (allFish.Contains(fish))
        {
            allFish.Remove(fish);
        }
    }
}