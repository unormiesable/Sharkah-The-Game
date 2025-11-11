using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SharkController : MonoBehaviour
{
    [Header("SHARK MOVEMENT")]
    public bool isControllerEnabled = true;
    public float maxMoveSpeed = 5f;
    public float boostMoveSpeed = 10f;
    public float acceleration = 10f;
    public float boostAcceleration = 20f;
    public float deceleration = 15f;
    public float fixedXPosition = 0f;

    private Vector3 currentMoveVelocity = Vector3.zero;
    private Rigidbody rb;
    private float sharkRadius;


    [Header("ROTATION : LEFT - RIGHT")]
    public float yawSpeed = 7f;
    public float rightAngle = 90f;
    public float leftAngle = -90f;
    

    [Header("ROTATION : UP - DOWN")]
    public float pitchSpeed = 7f;
    public float upAngle = -30f;
    public float downAngle = 30f;
    public float neutralAngle = 0f;
    public float turningPitchDampener = 0.5f;


    [Header("HEAD AND JAW MOVEMENT")]
    public GameObject HeadAim;
    public Transform HeadTarget_Proxy;
    public Transform HeadTarget_Neutral;


    public GameObject JawAim;
    public Transform JawTarget_Proxy;
    public Transform JawTarget_Neutral;
    
    public float headAimSpeed = 5f;
    public float jawAimSpeed = 5f;
    public float preySearchInterval = 1.0f;
    public float preySearchRadius = 15f;
    

    [Header("PREY DETECTION")]
    public float closeRangeDistance = 7f;
    public float closeRangeUpOffset = 1f;


    private DampedTransform headDampedTransform;
    private DampedTransform jawDampedTransform;
    private float searchTimer;
    private Transform currentPreyTarget = null; 


    [Header("TAIL MOVEMENT")]
    public GameObject TailAim;
    public float tailBaseFrequency = 20f;
    public float tailMinSpeed = 1f;
    public float tailMaxSpeed = 3f;
    public float amplitude = 1f;
    public Vector3 moveAxis = Vector3.right;
    private Vector3 startLocalPosition; 
    private float tailPhase = 0f;


    [Header("AUDIO SOURCES")]
    public AudioSource eatingAudioSource;
    public float minEatingPitch = 1.4f;
    public float maxEatingPitch = 1.8f;
    public float minEatingVolume = 0.8f;
    public float maxEatingVolume = 1f;


    private Vector3 targetEulerAngles;
    private float initialRoll;
    private bool isFacingRight = true;

    private Vector3 moveDirectionInput = Vector3.zero;
    private bool isBoostingInput = false;
    private float targetPitchInput = 0f;


    void Start()
    {
        Debug.Log("Shark Controller Detected");

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; 
            rb.useGravity = false;
        }
        
        startLocalPosition = TailAim.transform.localPosition; 
        
        targetEulerAngles = transform.eulerAngles;
        initialRoll = targetEulerAngles.z;
        isFacingRight = Mathf.Abs(targetEulerAngles.y - leftAngle) > 90f;

        if (HeadAim != null)
        {
            headDampedTransform = HeadAim.GetComponent<DampedTransform>();
        }
        if (JawAim != null)
        {
            jawDampedTransform = JawAim.GetComponent<DampedTransform>();
        }
        searchTimer = preySearchInterval; 
    }


    void Update()
    {
        if (isControllerEnabled)
        {
            GatherUserInput();
        }

        TailAnimation();
        UpdateAimProxies(); 

        searchTimer += Time.deltaTime;
        if (searchTimer >= preySearchInterval)
        {
            searchTimer = 0f;
            FindClosestPrey();
        }
    }

    void FixedUpdate()
    {
        if (isControllerEnabled)
        {
            ApplyMovement();
        }
    }


    void GatherUserInput()
    {
        isBoostingInput = Input.GetKey(KeyCode.LeftShift);
        bool isTurning = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);

        if (Input.GetKeyDown(KeyCode.A)) { isFacingRight = false; }
        if (Input.GetKeyDown(KeyCode.D)) { isFacingRight = true; }
        
        float currentUpAngle = upAngle;
        float currentDownAngle = downAngle;

        if (isTurning)
        {
            currentUpAngle *= turningPitchDampener;
            currentDownAngle *= turningPitchDampener;
        }

        if (Input.GetKey(KeyCode.W)) { targetPitchInput = currentUpAngle; }
        else if (Input.GetKey(KeyCode.S)) { targetPitchInput = currentDownAngle; }
        else { targetPitchInput = neutralAngle; }
        
        moveDirectionInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) { moveDirectionInput.y += 1; }
        if (Input.GetKey(KeyCode.S)) { moveDirectionInput.y -= 1; }
        if (Input.GetKey(KeyCode.A)) { moveDirectionInput.z -= 1; }
        if (Input.GetKey(KeyCode.D)) { moveDirectionInput.z += 1; }

        if (moveDirectionInput.magnitude > 1f)
        {
            moveDirectionInput.Normalize();
        }
    }


    void ApplyMovement()
    {
        float currentMaxSpeed = isBoostingInput ? boostMoveSpeed : maxMoveSpeed;
        float currentAccel;

        if (moveDirectionInput.magnitude > 0)
        {
            currentAccel = isBoostingInput ? boostAcceleration : acceleration;
        }
        else
        {
            currentAccel = deceleration;
        }

        Vector3 targetVelocity = moveDirectionInput * currentMaxSpeed;

        currentMoveVelocity = Vector3.MoveTowards(
            currentMoveVelocity, 
            targetVelocity, 
            currentAccel * Time.fixedDeltaTime
        );
        
        rb.linearVelocity = currentMoveVelocity;
        
        Vector3 newPosition = rb.position;
        newPosition.x = fixedXPosition;
        rb.position = newPosition;
        
        targetEulerAngles.x = Mathf.LerpAngle(targetEulerAngles.x, targetPitchInput, pitchSpeed * Time.fixedDeltaTime);
        float targetY = isFacingRight ? rightAngle : leftAngle;
        targetEulerAngles.y = Mathf.LerpAngle(targetEulerAngles.y, targetY, yawSpeed * Time.fixedDeltaTime);
        targetEulerAngles.z = Mathf.LerpAngle(
            targetEulerAngles.z,
            initialRoll,
            pitchSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(Quaternion.Euler(targetEulerAngles));
    }


    void TailAnimation()
    {
        float currentMaxSpeed = isBoostingInput ? boostMoveSpeed : maxMoveSpeed;
        float speedPercent = Mathf.InverseLerp(0, currentMaxSpeed, currentMoveVelocity.magnitude);
        float currentTailMultiplier = Mathf.Lerp(tailMinSpeed, tailMaxSpeed, speedPercent);

        tailPhase += (tailBaseFrequency * currentTailMultiplier) * Time.deltaTime;
        float oscillation = Mathf.Sin(tailPhase) * amplitude;

        Vector3 offset = moveAxis.normalized * oscillation;
        TailAim.transform.localPosition = startLocalPosition + offset; 
    }


    void FindClosestPrey()
    {
        if (HeadTarget_Neutral == null) return;

        Collider[] allCollidersInRadius = Physics.OverlapSphere(
            HeadTarget_Neutral.position, 
            preySearchRadius
        );

        Transform closestPrey = null;
        float minDistance = Mathf.Infinity;

        if (allCollidersInRadius.Length == 0)
        {
            currentPreyTarget = null;
            return;
        }

        foreach (Collider col in allCollidersInRadius)
        {
            PreyTarget prey = col.GetComponent<PreyTarget>();

            if (prey != null)
            {
                float distance = Vector3.Distance(transform.position, prey.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPrey = prey.transform;
                }
            }
        }
        
        currentPreyTarget = closestPrey;
    }


    void UpdateAimProxies()
    {
        float distanceToPrey = Mathf.Infinity;
        bool preyIsClose = false;

        if (currentPreyTarget != null)
        {
            distanceToPrey = Vector3.Distance(transform.position, currentPreyTarget.position);
            preyIsClose = (distanceToPrey < closeRangeDistance);
        }

        if (HeadTarget_Proxy != null && HeadTarget_Neutral != null)
        {
            Vector3 headTargetPosition;
            if (currentPreyTarget != null)
            {
                if (preyIsClose)
                {
                    headTargetPosition = currentPreyTarget.position + (transform.up * closeRangeUpOffset);
                }
                else
                {
                    headTargetPosition = currentPreyTarget.position;
                }
            }
            else
            {
                headTargetPosition = HeadTarget_Neutral.position;
            }

            HeadTarget_Proxy.position = Vector3.Lerp(
                HeadTarget_Proxy.position, 
                headTargetPosition, 
                headAimSpeed * Time.deltaTime
            );
        }

        if (JawTarget_Proxy != null && JawTarget_Neutral != null)
        {
            Vector3 jawTargetPosition;
            
            if (currentPreyTarget != null && (preyIsClose || HeadTarget_Proxy.position.y > 2.5))
            {
                jawTargetPosition = currentPreyTarget.position - (transform.up * 5);
            }

            else
            {
                jawTargetPosition = JawTarget_Neutral.position;
            }

            JawTarget_Proxy.position = Vector3.Lerp(
                JawTarget_Proxy.position,
                jawTargetPosition,
                jawAimSpeed * Time.deltaTime
            );
        }
    }


    void OnTriggerEnter(Collider other)
    {
        PreyTarget prey = other.GetComponent<PreyTarget>();

        if (prey != null)
        {
            Eat(prey.gameObject);
        }
    }


    void Eat(GameObject preyObject)
    {
        if (preyObject.transform == currentPreyTarget)
        {
            currentPreyTarget = null;
        }
        Destroy(preyObject);
        PlayEatingAudio();
    }


    void PlayEatingAudio()
    {
        eatingAudioSource.Stop();
        eatingAudioSource.pitch = Random.Range(minEatingPitch, maxEatingPitch);
        eatingAudioSource.volume = Random.Range(minEatingVolume, maxEatingVolume);
        eatingAudioSource.Play();
    }
}