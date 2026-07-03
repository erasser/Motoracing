using UnityEngine;

public class Player : MonoBehaviour
{
    public float forwardAcceleration = 800;
    [Tooltip("m/s")]
    public float maxSpeed = 50;
    Rigidbody _rb;
    public Transform applyForwardForceAtPosition;
    public Transform centerOfMass;
    public Transform frontWheel;
    public Transform rearWheel;
    public WheelCollider rearWheelCollider;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.maxLinearVelocity = maxSpeed;
        _rb.centerOfMass = centerOfMass.localPosition;
    }

    void Update()
    {
        ProcessControls();

        if (_rb.linearVelocity.sqrMagnitude < .04f)
            _rb.linearVelocity = transform.forward * .2f;

        UpdateWheelRotation();
    }

    void ProcessControls()
    {
        // TODO: Pokud je stisknutý nějaký control, testovat ground hit zde předem

        if (Input.GetKey(KeyCode.W))
        {
            _rb.AddForceAtPosition(Time.deltaTime * forwardAcceleration * Vector3.forward, applyForwardForceAtPosition.position, ForceMode.Acceleration);

            var grounded = rearWheelCollider.GetGroundHit(out var hit);
            if (grounded)
                _rb.angularVelocity = Vector3.zero;
        }
        if (Input.GetKey(KeyCode.S))
            _rb.linearVelocity = Vector3.zero;

    }

    void UpdateWheelRotation()
    {
        var deltaAngle = Vector3.Dot(_rb.linearVelocity, transform.forward) / .475f * Time.deltaTime;

        frontWheel.Rotate(deltaAngle * Mathf.Rad2Deg, 0, 0);
        rearWheel.Rotate(deltaAngle * Mathf.Rad2Deg, 0, 0);
    }
}
