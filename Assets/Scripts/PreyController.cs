using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PreyController : MonoBehaviour
{
    [HideInInspector]
    /*
        Disini ada 2 manager - FlockManager sama PopulationManager
        Tinggal Pilih aja sih sesuai kebutuhan :
            public FlockManager manager;
            public PopulationManager manager;
    */
    public PopulationManager manager;
    

    [HideInInspector]
    public float xCenterPosition; 
    [HideInInspector]
    public float xMoveRange = 1f; 

    [Header("Movement")]
    public float maxMoveSpeed = 4f;
    public float acceleration = 8f;
    public float deceleration = 10f;
    private Vector3 currentMoveVelocity = Vector3.zero;
    private Rigidbody rb;
    private float fishRadius;

    [Header("Flocking Rules")]
    public float perceptionRadius = 5f;
    public float avoidanceRadius = 1f;
    public float wanderTargetRadius = 2f; 

    [Range(0, 5)] public float alignmentWeight = 1f;
    [Range(0, 5)] public float cohesionWeight = 1f;
    [Range(0, 5)] public float separationWeight = 3f;
    [Range(0, 5)] public float wanderWeight = 1.5f;
    [Range(0, 5)] public float fleeWeight = 5f;

    [Header("Danger")]
    public float dangerRadius = 10f;
    private Transform sharkTransform;

    [Header("TAIL MOVEMENT")]
    public GameObject TailAim;
    public float tailBaseFrequency = 20f;
    public float tailMinSpeed = 1f;
    public float tailMaxSpeed = 3f;
    public float amplitude = 1f;
    public Vector3 moveAxis = Vector3.right;
    private Vector3 startLocalPosition; 
    private float tailPhase = 0f;

    [Header("Mesh Controller")]
    public GameObject fishMeshObject;
    private SkinnedMeshRenderer fishMeshRenderer;

    [Header("Animator Controller")]
    private Animator animator;
    public bool cullAnimator = true;

    void Start()
    {
        OptimizeMesh();
        SetupFish();
    }

    void SetupFish(){
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  
        rb.useGravity = false;
        
        if (TryGetComponent<SphereCollider>(out SphereCollider sphereCol))
        {
            fishRadius = sphereCol.radius;
        }
        else
        {
            fishRadius = 0.5f; 
        }
        
        SharkController shark = FindFirstObjectByType<SharkController>(); 
        if (shark != null)
        {
            sharkTransform = shark.transform;
        }

        if (TailAim != null)
        {
            startLocalPosition = TailAim.transform.localPosition;
        }
    }

    void OptimizeMesh(){
        if (fishMeshObject != null)
        {
            fishMeshRenderer = fishMeshObject.GetComponent<SkinnedMeshRenderer>();
        }
        else
        {
            fishMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        if (fishMeshRenderer != null)
        {
            fishMeshRenderer.updateWhenOffscreen = false;
        }
        else
        {
            Debug.LogWarning("SkinnedMeshRenderer tidak ditemukan!");
        }

        animator = GetComponentInChildren<Animator>();
        if (animator != null && cullAnimator)
        {
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }
    }

    void Update()
    {
        Vector3 flockingDirection = CalculateFlockingVector();
        MoveFish(flockingDirection);
        TailAnimation();
    }


    Vector3 CalculateFlockingVector()
    {
        if (manager == null) return Vector3.zero;

        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 wander = Vector3.zero;
        Vector3 flee = Vector3.zero;

        int neighbors = 0;
        List<PreyController> allFish = manager.allFish;

        if (sharkTransform != null)
        {
            float sharkDist = Vector3.Distance(transform.position, sharkTransform.position);
            
            if (sharkDist < dangerRadius)
            {
                flee = (transform.position - sharkTransform.position).normalized * fleeWeight;
            }
        }

        foreach (PreyController fish in allFish)
        {
            if (fish == this || fish == null) continue;

            float distance = Vector3.Distance(transform.position, fish.transform.position);

            if (distance < perceptionRadius)
            {
                alignment += fish.transform.forward; 
                cohesion += fish.transform.position;
                neighbors++;
            }
            
            if (distance < avoidanceRadius)
            {
                separation += (transform.position - fish.transform.position);
            }
        }

        if (neighbors > 0)
        {
            alignment = (alignment / neighbors).normalized * alignmentWeight;
            cohesion = (cohesion / neighbors - transform.position).normalized * cohesionWeight;
            separation = (separation / neighbors).normalized * separationWeight;
        }
        
        if (manager.wanderTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, manager.wanderTarget.position);
            
            if (distanceToTarget > wanderTargetRadius)
            {
                wander = (manager.wanderTarget.position - transform.position).normalized * wanderWeight;
            }
        }
        
        Vector3 finalDirection = alignment + cohesion + separation + wander + flee;
        return finalDirection;
    }


    void MoveFish(Vector3 targetDirection)
    {
        Vector3 targetVelocity = targetDirection.normalized * maxMoveSpeed;
        float currentAccel = (targetDirection.magnitude > 0) ? acceleration : deceleration;

        currentMoveVelocity = Vector3.MoveTowards(
            currentMoveVelocity,
            targetVelocity,
            currentAccel * Time.deltaTime
        );

        Vector3 moveThisFrame = currentMoveVelocity * Time.deltaTime;
        float moveDistance = moveThisFrame.magnitude;

        if (moveDistance > 0.001f)
        {
            if (Physics.SphereCast(transform.position, fishRadius, moveThisFrame.normalized, out RaycastHit hit, moveDistance))
            {
                Vector3 slideVelocity = Vector3.ProjectOnPlane(currentMoveVelocity, hit.normal);
                
                currentMoveVelocity = slideVelocity;
                
                moveThisFrame = currentMoveVelocity * Time.deltaTime;
            }

            Vector3 newPos = transform.position + moveThisFrame;
            newPos.x = Mathf.Clamp(newPos.x, xCenterPosition - xMoveRange, xCenterPosition + xMoveRange);
            transform.position = newPos; 
        }


        if (currentMoveVelocity.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentMoveVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }


    void TailAnimation()
    {
        if (TailAim == null) return; 

        float speedPercent = Mathf.InverseLerp(0, maxMoveSpeed, currentMoveVelocity.magnitude);
        float currentTailMultiplier = Mathf.Lerp(tailMinSpeed, tailMaxSpeed, speedPercent);

        tailPhase += (tailBaseFrequency * currentTailMultiplier) * Time.deltaTime;
        float oscillation = Mathf.Sin(tailPhase) * amplitude;

        Vector3 offset = moveAxis.normalized * oscillation;
        TailAim.transform.localPosition = startLocalPosition + offset; 
    }


    void OnDestroy()
    {
        if (manager != null)
        {
            manager.RemoveFish(this); 
        }
    }
}