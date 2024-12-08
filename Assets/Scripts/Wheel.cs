using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    public Rigidbody carRb;
    public float wheelRadius = 0.8f;
    public float maxTurnAngle = 30f;
    public WheelType wheelType;

    private Vector3 localWheelRotation = new Vector3(0f, 0f, 0f);
    
    public void AssignValues(Rigidbody carRb, float wheelRadius, float maxTurnAngle){
        this.carRb = carRb;
        this.wheelRadius = wheelRadius;
        this.maxTurnAngle = maxTurnAngle;
    }
    private void Update() {
        Move();
    }

    void RotateWheel(){
        float carSpeed = Vector3.Scale(carRb.velocity, carRb.transform.forward).magnitude;        
        float rotationAngle = CarPhysics.CalculateWheelTurnSpeed(carSpeed, wheelRadius);
        localWheelRotation.x += rotationAngle;
    }

    void TurnWheel(){
        float horizontalInput = Input.GetAxis("Horizontal");
        localWheelRotation.y = horizontalInput * maxTurnAngle;
    }

    public void Move(){
        if(localWheelRotation.x > 360) localWheelRotation.x = 0f;
        RotateWheel();
        if(wheelType == WheelType.frontWheel) TurnWheel();
        transform.localRotation = Quaternion.Euler(localWheelRotation);
    }
}

public enum WheelType{
    frontWheel,
    backWheel
}
