using UnityEngine;
using UnityEngine.UI;

// https://claude.ai/chat/fb6bcee5-bdcc-4a3f-a967-038c3e812785

public class Player : MonoBehaviour
{
    // [Tooltip("m/s")]
    // public float maxSpeed = 50;
    public float forwardAcceleration = 800;
    public Transform centerOfMass;
    public Transform applyForwardForceAtPosition;
    public Transform frontWheel;
    public Transform rearWheel;
    public Transform frontWheelTopAnchor;
    public Transform rearWheelTopAnchor;
    public float wheelRadius = .2f;
    Rigidbody _rb;
    float _dt;
    float _fdt;
    bool _keyForwardPressed;
    bool _keyLeftPressed;
    bool _keyRightPressed;
    public LayerMask groundLayerMask;
    public float springStrength = 30000;  // N/m -- síla, ne "acceleration" konstanta
    public float damperStrength = 3000;   // N*s/m
    WheelSuspension _frontWheelSuspension;
    WheelSuspension _rearWheelSuspension;
    Text _infoText;
    float _targetSteerAngle;
    float _steerAngle;
    [Header("Steering")]
    public float maxSteerAngle = 30;
    public float minSteerAngle = 8;       // úhel při vysoké rychlosti
    // TODO: Musim být schopný spočítat reálnou maximální rychlost
    public float referenceSpeed;           // m/s, rychlost, při které dosáhneš minSteerAngle
    public float steerSmoothSpeed = 5f;    // laditelná citlivost Lerpu
    public Transform handleBar;
    Vector3 _initialHandleBarEuler;
    [Header("Grip")]
    public float frontGripStrength = 1000;
    public float rearGripStrength = 4000;
    public float frontMaxGripForce = 4000;
    public float rearMaxGripForce = 3000;
    [Header("Roll Stabilization")]
    public float rollStiffness = 800;
    public float rollDamping = 150;
    [Header("Roll Stabilization")]
    public float leanResponsiveness = 1; // 0 = žádný náklon do zatáčky, 1 = plný fyzikální náklon

    void Start()
    {
        _infoText = GameObject.Find("info text").GetComponent<Text>();
        _initialHandleBarEuler = handleBar.localEulerAngles;
        // referenceSpeed = maxSpeed;  // TODO: odstranit referenceSpeed, jestli to tak nechám

        _rb = GetComponent<Rigidbody>();
        // _rb.maxLinearVelocity = maxSpeed;
        _rb.centerOfMass = centerOfMass.localPosition;

        var restLengthFront = frontWheelTopAnchor.position.y - frontWheel.position.y;
        var springTravelFront = restLengthFront - wheelRadius;  // Tohle by se správně mělo počítat jako POMĚR z restLengthFront. Ale já si ten anchor můžu nastavit, jak vysoko chci. 

        _frontWheelSuspension = new (frontWheelTopAnchor, wheelRadius, restLengthFront, springTravelFront, springStrength, damperStrength);
        _rearWheelSuspension = new (rearWheelTopAnchor, wheelRadius, restLengthFront, springTravelFront, springStrength, damperStrength);
    }

    void Update()
    {
        _dt = Time.deltaTime;

        ProcessControls();
    }

    void FixedUpdate()
    {
        _fdt = Time.fixedDeltaTime;

        UpdateWheelRotation();

        UpdateSteering();

        ApplyWheelSuspension(_frontWheelSuspension);
        ApplyWheelSuspension(_rearWheelSuspension);    

        ApplyLateralGrip(_frontWheelSuspension, handleBar.right, frontGripStrength, frontMaxGripForce);
        ApplyLateralGrip(_rearWheelSuspension, transform.right, rearGripStrength, rearMaxGripForce);

        ApplyLongitudinalForce(/*_rearWheelSuspension, transform.forward, forwardAcceleration*/);  // forward push

        ApplyRollStabilization();

        // _infoText.text = $"front normal: {_frontWheelSuspension.groundNormal}\nrear normal: {_rearWheelSuspension.groundNormal}";
    }

    void ProcessControls()
    {
        _keyForwardPressed = Input.GetKey(KeyCode.W);
        _keyLeftPressed = Input.GetKey(KeyCode.A);
        _keyRightPressed = Input.GetKey(KeyCode.D);
    }

    void ApplyLongitudinalForce()
    {
        if (_keyForwardPressed && _rearWheelSuspension.grounded)
            _rb.AddForceAtPosition(_fdt * forwardAcceleration * transform.forward, applyForwardForceAtPosition.position, ForceMode.Acceleration);
    }

    void ApplyLongitudinalForce(WheelSuspension wheel, Vector3 forwardDir, float force)
    {
        if (!wheel.grounded || Mathf.Approximately(force, 0f)) return;

        var dir = Vector3.ProjectOnPlane(forwardDir, wheel.groundNormal).normalized;
        _rb.AddForceAtPosition(dir * force, wheel.contactPoint);
    }

    void UpdateWheelRotation()  // TODO: To počítá i při letu/pádu
    {
        var deltaAngle = Vector3.Dot(_rb.linearVelocity, transform.forward) / .475f * _dt;

        frontWheel.Rotate(deltaAngle * Mathf.Rad2Deg, 0, 0);
        rearWheel.Rotate(deltaAngle * Mathf.Rad2Deg, 0, 0);
    }

    void ApplyWheelSuspension(WheelSuspension wheel/*, bool updateInfoText = false*/)
    {
        var origin = wheel.WheelAnchor.position;
        var down = - transform.up; // lokální dolů, sleduje náklon motorky
        var maxDist = wheel.RestLength + wheel.SpringTravel + wheel.WheelRadius;

        if (Physics.Raycast(origin, down, out var hit, maxDist, groundLayerMask))
        {
            wheel.grounded = true;
            wheel.contactPoint = hit.point;
            wheel.groundNormal = hit.normal;

            // Aktuální délka pružiny (vzdálenost od anchoru k bodu, kde by "sedělo" kolo)
            var currentLength = hit.distance - wheel.WheelRadius;
            currentLength = Mathf.Clamp(currentLength, 0f, wheel.RestLength + wheel.SpringTravel);

            var compression = (wheel.RestLength - currentLength) / wheel.SpringTravel; 
            // compression > 0 = pružina stlačená = tlačí ven

            // Rychlost stlačování/rozpínání (damping)
            var velocity = (wheel.lastLength - currentLength) / Time.fixedDeltaTime;
            wheel.lastLength = currentLength;

            var springForce = compression * wheel.SpringStrength;
            var damperForce = velocity * wheel.DamperStrength;

            var totalForce = springForce + damperForce;
            totalForce = Mathf.Max(totalForce, 0f); // pružina nikdy netahá dolů, jen tlačí

            _rb.AddForceAtPosition(transform.up * totalForce, origin);
        }
        else
        {
            wheel.grounded = false;
            wheel.lastLength = wheel.RestLength + wheel.SpringTravel;
        }
    }

    void UpdateSteering()
    {
        // TODO: Až budou smyky nagradit vel.magnitude za float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        var speedFactor = Mathf.Clamp01(_rb.linearVelocity.magnitude / referenceSpeed);
        var currentMaxSteerAngle = Mathf.Lerp(maxSteerAngle, minSteerAngle, speedFactor);

        if (_keyLeftPressed)
            _targetSteerAngle = -currentMaxSteerAngle;
        else if (_keyRightPressed)
            _targetSteerAngle = currentMaxSteerAngle;
        else
            _targetSteerAngle = 0f;

        _steerAngle = Mathf.Lerp(_steerAngle, _targetSteerAngle, _fdt * steerSmoothSpeed);
        _steerAngle = Mathf.Clamp(_steerAngle, -maxSteerAngle, maxSteerAngle);

        handleBar.localEulerAngles = new Vector3(_initialHandleBarEuler.x, _steerAngle, _initialHandleBarEuler.z);
        
        // _infoText.text = $"steer ang: {_steerAngle}\nvelocity: {_rb.linearVelocity.magnitude}";
    }

    class WheelSuspension
    {
        public readonly Transform WheelAnchor;      // bod na motorce, odkud vede raycast (osa vidlice)
        public readonly float WheelRadius;
        public readonly float RestLength;    // klidová délka odpružení
        public readonly float SpringTravel;  // maximální komprese
        public readonly float SpringStrength; // N/m -- síla, ne "acceleration" konstanta
        public readonly float DamperStrength;  // N*s/m
        public Vector3 groundNormal;

        public WheelSuspension(Transform anchor, float radius, float restLength, float springTravel, float springStrength, float damperStrength)
        {
            WheelAnchor = anchor;
            WheelRadius = radius;
            RestLength = restLength;
            SpringTravel = springTravel;
            SpringStrength = springStrength;
            DamperStrength = damperStrength;
        }

        public float lastLength; // pro výpočet compression velocity
        public bool grounded;
        public Vector3 contactPoint;
    }

    // P-controller
    void ApplyLateralGrip(WheelSuspension wheel, Vector3 rightDir, float gripStrength, float maxGripForce)
    {
        if (!wheel.grounded) return;
    
        var right = Vector3.ProjectOnPlane(rightDir, wheel.groundNormal).normalized;
        var pointVelocity = _rb.GetPointVelocity(wheel.contactPoint);
        var lateralSpeed = Vector3.Dot(pointVelocity, right);

        var force = -lateralSpeed * gripStrength; // NE / Time.fixedDeltaTime
        force = Mathf.Clamp(force, -maxGripForce, maxGripForce);

        _rb.AddForceAtPosition(right * force, wheel.contactPoint);

        // if(info)
            // _infoText.text = $"handle R: {handleBar.right}";
    }

    void ApplyRollStabilization()
    {
        var forward = transform.forward;
        var projectedWorldUp = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;
        var projectedLocalUp = Vector3.ProjectOnPlane(transform.up, forward).normalized;

        var rollAngle = Vector3.SignedAngle(projectedWorldUp, projectedLocalUp, forward); // stupně
        var rollRate = Vector3.Dot(_rb.angularVelocity, forward); // rad/s

        // Yaw rate kolem SVISLÉ osy (world up), ne kolem forward
        var yawRate = Vector3.Dot(_rb.angularVelocity, Vector3.up); // rad/s
        var speed = Vector3.Dot(_rb.linearVelocity, forward); // dopředná rychlost

        var targetRollAngle = - Mathf.Atan2(speed * yawRate, 9.81f) * Mathf.Rad2Deg;
        targetRollAngle *= leanResponsiveness;

        var error = rollAngle - targetRollAngle;
        var torque = - error * Mathf.Deg2Rad * rollStiffness - rollRate * rollDamping;

        _rb.AddTorque(forward * torque);
    }
}
