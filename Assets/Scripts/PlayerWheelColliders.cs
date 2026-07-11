using UnityEngine;

public class PlayerWheelColliders : MonoBehaviour
{
    public float forwardAcceleration = 800;
    [Tooltip("m/s")]
    public float maxSpeed = 50;
    Rigidbody _rb;
    public Transform applyForwardForceAtPosition;
    public Transform centerOfMass;
    public Transform frontWheel;
    public Transform rearWheel;
    public WheelCollider frontWheelCollider;
    public WheelCollider rearWheelCollider;
    float _dt;
    float _fdt;
    float _targetYDeltaRotation;
    public float maxRotationDelta = 20;
    public float rotationIncrement = 2;
    bool keyForwardPressed;
    bool keyLeftPressed;
    bool keyRightPressed;
    
    float _targetYaw;
    float _yawDelta;

    public float yawSpeed = 90f;      // rychlost přidávání cílového úhlu
    public float yawAcc = 6f;         // jak rychle se motorka přibližuje k cílovému úhlu
    public float yawTorque = 50f;     // síla torquu kolem Y

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.maxLinearVelocity = maxSpeed;
        _rb.centerOfMass = centerOfMass.localPosition;
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
    }

    void ProcessControls()
    {
        keyForwardPressed = Input.GetKey(KeyCode.W);
        keyLeftPressed = Input.GetKey(KeyCode.A);
        keyRightPressed = Input.GetKey(KeyCode.D);
    }

    void ProcessFixedControls()
    {
        // TODO: Pokud je stisknutý nějaký control, testovat ground hit zde předem

        if (keyForwardPressed)
        {
            _rb.AddForceAtPosition(_fdt * forwardAcceleration * transform.forward, applyForwardForceAtPosition.position, ForceMode.Acceleration);

            var grounded = rearWheelCollider.GetGroundHit(out var hit);
            if (grounded)
                _rb.angularVelocity = Vector3.zero;
        }

        if (keyLeftPressed)
            frontWheelCollider.steerAngle -= _fdt * rotationIncrement;
        else if (keyRightPressed)
            frontWheelCollider.steerAngle += _fdt * rotationIncrement;
        
        frontWheelCollider.steerAngle = Mathf.Clamp(frontWheelCollider.steerAngle, - 40, 40);

        // Debug.Log(frontWheelCollider.steerAngle);
    }

    void UpdateWheelRotation()
    {
        var deltaAngle = Vector3.Dot(_rb.linearVelocity, transform.forward) / .475f * _dt;

        frontWheel.Rotate(deltaAngle * Mathf.Rad2Deg, 0, 0);
        rearWheel.Rotate(deltaAngle * Mathf.Rad2Deg, 0, 0);
    }
}
