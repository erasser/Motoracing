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
    public WheelSuspension _frontWheelSuspension;
    WheelSuspension _rearWheelSuspension;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.maxLinearVelocity = maxSpeed;
        _rb.centerOfMass = centerOfMass.localPosition;
        _frontLocalOffset = frontWheel.position - transform.position;
        _rearLocalOffset = rearWheel.position - transform.position;
        _frontWheelSuspension = new WheelSuspension(frontWheel, .2f);
        _rearWheelSuspension = new WheelSuspension(rearWheel, .2f);
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

        // if (_rb.linearVelocity.sqrMagnitude < .04f)
        //     _rb.linearVelocity = transform.forward * .2f;

        ProcessFixedControls();

        ApplyWheelSuspension(_frontWheelSuspension);
        ApplyWheelSuspension(_rearWheelSuspension);        
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
        public float restLength = .4f;    // klidová délka odpružení
        public float springTravel = .2f;  // maximální komprese
        public float wheelRadius;

        public float springStrength = 30000; // N/m -- síla, ne "acceleration" konstanta
        public float damperStrength = 3000;  // N*s/m

        public WheelSuspension(Transform anchor, float radius)
        {
            wheelAnchor = anchor;
            wheelRadius = radius;
        }

        [HideInInspector] public float lastLength; // pro výpočet compression velocity
        [HideInInspector] public bool grounded;
        [HideInInspector] public Vector3 contactPoint;
    }
}
