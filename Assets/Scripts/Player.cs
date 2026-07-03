using UnityEngine;

public class Player : MonoBehaviour
{
    public float forwardAcceleration;
    [Tooltip("m/s")]
    public float maxSpeed;
    Rigidbody _rb;

    public Transform frontWheel;
    public Transform rearWheel;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.maxLinearVelocity = maxSpeed;
    }

    void Update()
    {
        // ProcessControls();        
    }

    void ProcessControls()
    {
        if (Input.GetKey(KeyCode.W))
            _rb.AddForce(Time.deltaTime * forwardAcceleration * Vector3.forward, ForceMode.Acceleration);
    }
}
