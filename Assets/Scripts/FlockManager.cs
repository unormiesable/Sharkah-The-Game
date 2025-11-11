using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    [Header("Flock Settings")]
    public PreyController fishPrefab;
    public int fishCount = 20;
    public Vector3 spawnBounds = new Vector3(1f, 10f, 10f); 
    
    public float minScaleIncrease = 0.0f;
    public float maxScaleIncrease = 0.5f;

    [Header("Wander Target")]
    public Transform wanderTarget;
    public float wanderTargetSpeed = 1f;
    public float wanderTargetBounds = 10f;
    private Vector3 initialWanderPosition;

    [Header("Shark Avoidance")]
    public float sharkAvoidanceRadius = 25f;
    public float fleeSpeed = 3f;
    private Transform sharkTransform;

    [HideInInspector]
    public List<PreyController> allFish = new List<PreyController>();
    
    private float fixedXPosition;

    void Start()
    {
        fixedXPosition = transform.position.x;

        for (int i = 0; i < fishCount; i++)
        {
            Vector3 randomPos = new Vector3(
                fixedXPosition + Random.Range(-spawnBounds.x, spawnBounds.x),
                transform.position.y + Random.Range(-spawnBounds.y, spawnBounds.y),
                transform.position.z + Random.Range(-spawnBounds.z, spawnBounds.z)
            );
            
            PreyController fish = Instantiate(fishPrefab, randomPos, Quaternion.identity);

            Vector3 baseScale = fish.transform.localScale;
            float randomAddition = Random.Range(minScaleIncrease, maxScaleIncrease);
            
            fish.transform.localScale = baseScale + Vector3.one * randomAddition;
            
            // Comment Dlu biar non aktif karena mau pake PopulationManager
            // fish.manager = this; 
            fish.xCenterPosition = this.fixedXPosition; 
            fish.xMoveRange = this.spawnBounds.x;
            allFish.Add(fish);
        }

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
    }

    void Update()
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
}