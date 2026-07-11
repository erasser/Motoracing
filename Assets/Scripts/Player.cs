using UnityEngine;
// https://claude.ai/chat/fb6bcee5-bdcc-4a3f-a967-038c3e812785

public class Player : MonoBehaviour
{
    [Tooltip("m/s")]
    public float maxSpeed = 50;
    public float forwardAcceleration = 800;
    public Transform centerOfMass;
    public Transform applyForwardForceAtPosition;
    public Transform frontWheel;
    public Transform rearWheel;
    public Transform frontWheelBottomPoint;
    public Transform rearWheelBottomPoint;
    public float wheelRadius = .5f;
    Rigidbody _rb;
    float _dt;
    float _fdt;
    bool _keyForwardPressed;
    bool _keyLeftPressed;
    bool _keyRightPressed;
    public LayerMask groundLayerMask;
    Vector3 _frontLocalOffset;
    Vector3 _rearLocalOffset;
    public float stiffness = 80;
    public float damping = 18;
    public float rotStiffness = 150;
    public float rotDamping = 25;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.maxLinearVelocity = maxSpeed;
        _rb.centerOfMass = centerOfMass.localPosition;
        _frontLocalOffset = frontWheel.position - transform.position;
        _rearLocalOffset = rearWheel.position - transform.position;
    }

    void Update()
    {
        _dt = Time.deltaTime;

        ProcessControls();

        UpdateWheelRotation();
    }

    void FixedUpdate()
    {
        _fdt = Time.fixedDeltaTime;

        if (_rb.linearVelocity.sqrMagnitude < .04f)
            _rb.linearVelocity = transform.forward * .2f;

        ProcessFixedControls();

        ApplyWheelSuspension(frontWheel);  // TODO: Zde jsem skončil. Poslední nedodělaný pokus. Asi se po mně chce tam podstrčit instance WheelSuspension.
        ApplyWheelSuspension(rearWheel);        
    }
    void ProcessControls()
    {
        _keyForwardPressed = Input.GetKey(KeyCode.W);
        _keyLeftPressed = Input.GetKey(KeyCode.A);
        _keyRightPressed = Input.GetKey(KeyCode.D);
    }

    void ProcessFixedControls()
    {
        if (_keyForwardPressed)
        {
            _rb.AddForceAtPosition(_fdt * forwardAcceleration * transform.forward, applyForwardForceAtPosition.position, ForceMode.Acceleration);
        }

        // if (_keyLeftPressed)
        //     frontWheelCollider.steerAngle -= _fdt * rotationIncrement;
        // else if (_keyRightPressed)
        //     frontWheelCollider.steerAngle += _fdt * rotationIncrement;
        
        // frontWheelCollider.steerAngle = Mathf.Clamp(frontWheelCollider.steerAngle, - 40, 40);

        // Debug.Log(frontWheelCollider.steerAngle);
    }

    void UpdateWheelRotation()
    {
        var deltaAngle = Vector3.Dot(_rb.linearVelocity, transform.forward) / .475f * _dt;

        frontWheel.Rotate(deltaAngle * Mathf.Rad2Deg, 0, 0);
        rearWheel.Rotate(deltaAngle * Mathf.Rad2Deg, 0, 0);
    }

    void UpdateTransform()
    {
        // TODO: SphereCast
        Physics.Raycast(frontWheel.position, - transform.up, out var frontHit, groundLayerMask);
        Physics.Raycast(rearWheel.position, - transform.up, out var rearHit, groundLayerMask);

        // "Je to aproximace – přesné by to bylo jen na rovině kolmé k normále, ale pro běžné svahy je to dostatečně přesné." - Co to mele DO PÍČI?
        var frontWheelCenter = frontHit.point + frontHit.normal * wheelRadius;
        var rearWheelCenter = rearHit.point + rearHit.normal * wheelRadius;

        var forward = (frontWheelCenter - rearWheelCenter).normalized;
        var upReference = (frontHit.normal + rearHit.normal).normalized;
        var right = Vector3.Cross(upReference, forward).normalized;
        var up = Vector3.Cross(forward, right).normalized;

        var targetRotation = Quaternion.LookRotation(forward, up);
        var originFromFront = frontWheelCenter - targetRotation * _frontLocalOffset;
        var originFromRear  = rearWheelCenter  - targetRotation * _rearLocalOffset;

        var targetPosition = Vector3.Lerp(originFromFront, originFromRear, .5f);

        Vector3 posError = targetPosition - _rb.position;
        var force = posError * stiffness - _rb.linearVelocity * damping;
        _rb.AddForce(force, ForceMode.Acceleration);
        Debug.Log($"force: {force}");
  
        Quaternion rotError = targetRotation * Quaternion.Inverse(_rb.rotation);
        rotError.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        _rb.AddTorque(axis * (angle * Mathf.Deg2Rad) * rotStiffness - _rb.angularVelocity * rotDamping, ForceMode.Acceleration);
    }

    void UpdateTransform2()  // kinematické řešení - to možná vyžaduje vypnutou gravitaci a IsKinematic?
    {
        Physics.Raycast(frontWheel.position, - transform.up, out var frontHit, groundLayerMask);
        Physics.Raycast(rearWheel.position, - transform.up, out var rearHit, groundLayerMask);

        // 1) Zachováme aktuální yaw (Y) a roll (Z, lean) – měníme jen pitch (X)
        Vector3 currentEuler = transform.eulerAngles;

        Quaternion yawOnly = Quaternion.Euler(0f, currentEuler.y, 0f);
        Vector3 flatForward = yawOnly * Vector3.forward; // dopředný směr bez pitch/roll

        // 2) Pitch z rozdílu výšek kontaktních bodů
        Vector3 diff = frontHit.point - rearHit.point;

        float forwardDist = Vector3.Dot(diff, flatForward); // vzdálenost "dopředu"
        float heightDist  = diff.y;                          // výškový rozdíl

        float pitchAngle = -Mathf.Atan2(heightDist, forwardDist) * Mathf.Rad2Deg;
        // Znaménko (-) obrať, pokud se motorka naklání opačným směrem než chceš

        Quaternion targetRotation = Quaternion.Euler(pitchAngle, currentEuler.y, currentEuler.z);

        // 3) Pozice dopočtená ze dvou kontaktních bodů + lokálních offsetů
        Vector3 originFromFront = frontHit.point - targetRotation * _frontLocalOffset;
        Vector3 originFromRear  = rearHit.point  - targetRotation * _rearLocalOffset;

        Vector3 targetPosition = (originFromFront + originFromRear) * 0.5f;

        // 4) Přímé nastavení - žádné síly
        _rb.MovePosition(targetPosition);
        _rb.MoveRotation(targetRotation);
    }
    
    void ApplyWheelSuspension(WheelSuspension wheel)
    {
        Vector3 origin = wheel.wheelAnchor.position;
        Vector3 down = -transform.up; // lokální dolů, sleduje náklon motorky

        float maxDist = wheel.restLength + wheel.springTravel + wheel.wheelRadius;

        if (Physics.Raycast(origin, down, out RaycastHit hit, maxDist))
        {
            wheel.grounded = true;
            wheel.contactPoint = hit.point;

            // Aktuální délka pružiny (vzdálenost od anchoru k bodu, kde by "sedělo" kolo)
            float currentLength = hit.distance - wheel.wheelRadius;
            currentLength = Mathf.Clamp(currentLength, 0f, wheel.restLength + wheel.springTravel);

            float compression = (wheel.restLength - currentLength) / wheel.springTravel; 
            // compression > 0 = pružina stlačená = tlačí ven

            // Rychlost stlačování/rozpínání (damping)
            float velocity = (wheel.lastLength - currentLength) / Time.fixedDeltaTime;
            wheel.lastLength = currentLength;

            float springForce = compression * wheel.springStrength;
            float damperForce = velocity * wheel.damperStrength;

            float totalForce = springForce + damperForce;
            totalForce = Mathf.Max(totalForce, 0f); // pružina nikdy netahá dolů, jen tlačí

            _rb.AddForceAtPosition(transform.up * totalForce, origin);
        }
        else
        {
            wheel.grounded = false;
            wheel.lastLength = wheel.restLength + wheel.springTravel;
        }
    }

    [System.Serializable]
    public class WheelSuspension
    {
        public Transform wheelAnchor;      // bod na motorce, odkud vede raycast (osa vidlice)
        public float restLength = 0.4f;    // klidová délka odpružení
        public float springTravel = 0.2f;  // maximální komprese
        public float wheelRadius = 0.3f;

        public float springStrength = 30000f; // N/m -- síla, ne "acceleration" konstanta
        public float damperStrength = 3000f;  // N*s/m

        [HideInInspector] public float lastLength; // pro výpočet compression velocity
        [HideInInspector] public bool grounded;
        [HideInInspector] public Vector3 contactPoint;
    }
}
