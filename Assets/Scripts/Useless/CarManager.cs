using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    public List<Wheel> wheels;
    public Transform centreOfGravity, frontAxel, rearAxel;
    public float dragConstant = 0.3f;
    public float frontalArea = 13f;
    public float rollingResistanceConstant = 0.02f;
    public float roadFrictionCoefficient = 0.8f;
    // public float frictionCoefficient = 1.3f;
    public float corneringStiffness = 0.13f;
    public float maxTurnAngle = 75f;
    public float wheelRadius = 0.86f;
    public float moveForce = 100f;
    public float mass = 1670f;
    public AnimationCurve accellerationCurve;
    
    private Rigidbody carRb;
    private float distanceFromCGToFrontAxel, distanceFromCGToRearAxel;

    private float steeringAngle;
    private float angularSpeed;
    private float lateralVelocity;
    private float longitudinalVelocity;
    private float frontSlipAngle;
    private float rearSlipAngle;
    private float maxNormalizedFrontFrictionForce;
    private float maxNormalizedRearFrictionForce;
    private float frontLateralForce;
    private float rearLateralForce;
    private Vector3 carVelocity;

    void Start(){
        carRb = this.GetComponent<Rigidbody>();
        distanceFromCGToFrontAxel = Mathf.Abs(frontAxel.transform.position.z - centreOfGravity.position.z);
        distanceFromCGToRearAxel = Mathf.Abs(rearAxel.transform.position.z - centreOfGravity.position.z);
        foreach(Wheel wheel in wheels){
            wheel.AssignValues(carRb, wheelRadius, maxTurnAngle);
        }
    }

    void FixedUpdate(){
        // UpdateValues();
        // MoveCar();  
        // MoveWheels();
        // addFriction();
        frontSlipAngle = CarPhysics.CalculateFrontWheelSlipAngle(angularSpeed, lateralVelocity, longitudinalVelocity, distanceFromCGToFrontAxel, steeringAngle);
        rearSlipAngle = CarPhysics.CalculateRearWheelSlipAngle(angularSpeed, lateralVelocity, longitudinalVelocity, distanceFromCGToFrontAxel);

    }

    void Update(){

    }

    void MoveCar(){
        // Vector3 netForce = getTotalForce();
        // Vector3 accelleration = CarPhysics.CalculateAcceleration(netForce, mass);
        // float timeIncrementBetweenPhysicsUpdates = Time.fixedDeltaTime;
        // Vector3 newVelocity = CarPhysics.CalculateVelocity(carRb.velocity, timeIncrementBetweenPhysicsUpdates, accelleration);
        // carRb.velocity += newVelocity;
        carRb.AddForce(carRb.transform.forward * moveForce);
        float corneringFoce = CarPhysics.CalculateCorneringForce(frontSlipAngle, corneringStiffness);
        carRb.AddForce(carRb.transform.right * -corneringFoce);
        transform.RotateAround(rearAxel.position, (Vector3.up), -corneringFoce / 7f);
        Debug.Log( corneringFoce) ;
    }

    void MoveWheels(){
        foreach(Wheel wheel in wheels){
            wheel.Move();
        }
    }


    Vector3 getTotalForce(){
        Vector3 velocity = carRb.velocity;
        Vector3 drag = CarPhysics.CalculateDrag(frontalArea, dragConstant, velocity);
        Vector3 rollingResistance = CarPhysics.CalculateRollingResistance(rollingResistanceConstant, velocity);
        Vector3 totalVelocity = velocity + drag + rollingResistance;
        return totalVelocity;
    }
    
    void UpdateValues(){
        steeringAngle = Input.GetAxis("Horizontal") * maxTurnAngle;
        angularSpeed = Vector3.Scale(carRb.angularVelocity, transform.forward).magnitude;
        lateralVelocity = Vector3.Scale(carRb.velocity, transform.forward).magnitude;
        longitudinalVelocity = Vector3.Scale(carRb.velocity, transform.right).magnitude;
        
        maxNormalizedFrontFrictionForce = CarPhysics.CalculateMaxNormalizedFrictionForce(roadFrictionCoefficient, distanceFromCGToRearAxel / (distanceFromCGToRearAxel + distanceFromCGToFrontAxel));
        maxNormalizedRearFrictionForce = CarPhysics.CalculateMaxNormalizedFrictionForce(roadFrictionCoefficient, distanceFromCGToFrontAxel / (distanceFromCGToRearAxel + distanceFromCGToFrontAxel));
        frontLateralForce = CarPhysics.CalculateLateralForce(corneringStiffness, frontSlipAngle, maxNormalizedFrontFrictionForce, mass * 9.81f);
        rearLateralForce = CarPhysics.CalculateLateralForce(corneringStiffness, rearSlipAngle, maxNormalizedRearFrictionForce, mass * 9.81f);
    }

    void addFriction(){
        carRb.AddForce(Vector3.Scale(carRb.velocity, transform.forward) * -rollingResistanceConstant);
        carRb.AddForce(carRb.velocity * -roadFrictionCoefficient);

    }
}
