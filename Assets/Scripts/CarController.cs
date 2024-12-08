using UnityEngine;

public class CarController : MonoBehaviour
{
    public float maxSpeed = 150f;
    public float maxAcceleration = 50f;
    private float currentAcelleration;
    public float reverseAcceleration = 40f;

    public float forwardDrag = 5f;
    public float sidewaysDrag = 15f;

    public float momentumAlignmentFactor = 0.1f;

    public float slipPreventAmplifier = 3f;
    public float minSlipTriggerSpeed = 12f;
    public float slipSteeringInfluence = 0.5f;

    public float brakingSteeringReduction = 0.5f;
    public float brakingPower = 70f;

    public float highSpeedSteeringReduction = 0.35f;
    public float baseSteeringPower = 180f;
    public float steeringSensitivity = 110;
    
    public float minimumMovementThreshold = 2f;
    public float minSpeedToStopCar = 1f; 

    public float rpmDecayRate = 3000f;
    public float rpmGrowthRate = 1500f;
    public float[] acellerationMultiplierForEachGear = { 4.0f, 2.5f, 1.5f, 1.0f, 0.75f };
    public int gearIndex = 0;
    public float maxEngineRPM = 7000f;
    public float minEngineRPMtoDownshift = 4200f;

    public float engineRPM;
    public bool isAutoTransmission = true;

    public Vector3 centerOfMassOffset = new Vector3(0, 0, -4f); 
    public float customGravityScale = 9.81f; 

    public TrailRenderer[] trails;

    public float carSpeed;
    private Rigidbody rb;
    private bool isSlipping = false; 
    private float turnEntrySpeed;       
    private bool isTurning = false;     
    public float turnSpeedClampThreshold = 20f; 
    public float applySkidMarksMinSLatpeed = 13f;

    private bool applySkidMarks = false; 

    public float getMaxCarSpeed() => maxSpeed;
    public float getCurrentCarSpeed() => carSpeed;

    private float driveInput;
    private float turnInput;
    private CarActionSpace actionSpace = new CarActionSpace();
    public bool doesHumanControll;


    public void setActionSpace(int currentActionIndex){
        actionSpace.setAction(currentActionIndex);
    }

    public void resetActionSpace()
    {
        actionSpace.Reset();
    }

    public bool isBreakPressed(){
        if(doesHumanControll) return Input.GetKey(KeyCode.Space);
        else return actionSpace.getActions()[3];
    }

    public void manageInputs(){
        if(doesHumanControll) {
            driveInput = Input.GetAxis("Vertical");
            if(driveInput < 0) turnInput = -Input.GetAxis("Horizontal");
            else turnInput = Input.GetAxis("Horizontal");

            return;
        }
        if(actionSpace.getActions()[0]) {
            driveInput = 1f;
            turnInput = 0;
        }else if(actionSpace.getActions()[1]){
            turnInput = 1f;
            driveInput = 1f;
        }else if(actionSpace.getActions()[2]){
            turnInput = -1f;
            driveInput = 1f;
        }else if(!actionSpace.getActions()[3]){
            driveInput = 0;
            turnInput = 0;
        }
        

    }



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += centerOfMassOffset; 
    }

    void Update()
    {

        manageInputs();
        CreateWheelEffects();
    }

    private void LateUpdate() {
        if (Input.GetAxis("Vertical") == 0 && rb.velocity.magnitude < minSpeedToStopCar)
        {
            StopCar();
        }
    }

    private void FixedUpdate() 
    {
        ApplyCustomGravity();
        ManageGearShifting();

        

        if (rb.velocity.magnitude < minimumMovementThreshold) turnInput = 0f;

        // Detect if car is entering a turn
        if (Mathf.Abs(turnInput) > 0 && !isTurning)
        {
            isTurning = true;
            turnEntrySpeed = rb.velocity.magnitude; // Store speed only once at the start of the turn
        }
        else if (Mathf.Abs(turnInput) == 0 && isTurning)
        {
            isTurning = false; // Exit turn
        }

        ApplyDriveForce();
        HandleBraking();
        ApplyMovementDrag();

        // Clamp speed during turning if it exceeds the turnEntrySpeed and is above the threshold
        if (isTurning && rb.velocity.magnitude > turnEntrySpeed && turnEntrySpeed > turnSpeedClampThreshold)
        {
            rb.velocity = rb.velocity.normalized * turnEntrySpeed;
        }

        if (rb.velocity.magnitude > minimumMovementThreshold || !Input.GetKey(KeyCode.Space))
        {
            ApplySteering(transform.InverseTransformDirection(rb.velocity).z < 0 ? -turnInput : turnInput);
        }
    }

    void ApplyDriveForce()
    {
        carSpeed = rb.velocity.magnitude;

        // Limit acceleration if car speed exceeds maxSpeed or, if turning, exceeds turnEntrySpeed above threshold
        if (carSpeed > maxSpeed || (isTurning && carSpeed > turnEntrySpeed && turnEntrySpeed > turnSpeedClampThreshold)) return;

        Vector3 driveDirection = driveInput > 0 ? transform.forward : -transform.forward;
        float driveForce = driveInput > 0 ? currentAcelleration : reverseAcceleration;

        rb.AddForce(driveDirection * driveForce * Mathf.Abs(driveInput), ForceMode.Acceleration);
    }

    void HandleBraking()
    {
        if (isBreakPressed())
        {
            Vector3 brakingForce = -rb.velocity.normalized * brakingPower;
            rb.AddForce(brakingForce, ForceMode.Acceleration);
        }
    }

    void ApplyMovementDrag()
    {
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float lateralSpeed = Vector3.Dot(rb.velocity, transform.right);

        if (forwardSpeed > 0)
        {
            rb.AddForce(-transform.forward * forwardDrag, ForceMode.Acceleration);
        }

        if (Mathf.Abs(lateralSpeed) > 0)
        {
            rb.AddForce(-transform.right * sidewaysDrag * Mathf.Sign(lateralSpeed), ForceMode.Acceleration);
        }
    }

    void StopCar()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero; 
    }

    void ApplySteering(float turnInput)
    {
        carSpeed = rb.velocity.magnitude;
        float speedAdjustedSteeringPower = baseSteeringPower * (1 - Mathf.Clamp01(carSpeed / maxSpeed) * highSpeedSteeringReduction);

        float lateralSpeed = Vector3.Dot(rb.velocity, transform.right);
        isSlipping = Mathf.Abs(lateralSpeed) > minSlipTriggerSpeed; 
        applySkidMarks =  Mathf.Abs(lateralSpeed) > applySkidMarksMinSLatpeed;

        if (isSlipping)
        {
            ApplySlipPrevention(lateralSpeed); 
        }

        Vector3 steeringForce = transform.right * turnInput * speedAdjustedSteeringPower * Time.deltaTime;
        if (Input.GetKey(KeyCode.Space))
        {
            steeringForce *= (1 - brakingSteeringReduction);
        }
        rb.AddForce(steeringForce, ForceMode.Acceleration);

        AlignMomentum();
        RotateCar();
    }

    void ApplySlipPrevention(float lateralSpeed) 
    {
        if ((lateralSpeed > 0 && turnInput < 0) || (lateralSpeed < 0 && turnInput > 0))
        {
            rb.AddForce(-transform.right * Mathf.Sign(lateralSpeed) * Mathf.Abs(lateralSpeed) * slipPreventAmplifier, ForceMode.Acceleration); 
        }
    }

    void AlignMomentum()
    {
        if (rb.velocity.magnitude > minimumMovementThreshold)
        {
            Vector3 velocityDirection = rb.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(velocityDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, momentumAlignmentFactor * Time.deltaTime);
        }
    }


    void RotateCar()
    {
        float speedAdjustedRotationFactor = steeringSensitivity * Mathf.Pow((1 - Mathf.Clamp01(carSpeed / maxSpeed) * highSpeedSteeringReduction), 2);

        float rotationAmount = turnInput * speedAdjustedRotationFactor * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, rotationAmount, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    void ManageGearShifting()
    {
        if (!isAutoTransmission) return;
        float totalRPM = maxEngineRPM + (maxEngineRPM - minEngineRPMtoDownshift) * (acellerationMultiplierForEachGear.Length);
        float rpmPerSpeed = totalRPM / maxSpeed;
        float currentTotalRPM = rpmPerSpeed * carSpeed;
        currentTotalRPM = Mathf.Clamp(currentTotalRPM, -totalRPM, totalRPM-1);
        if(currentTotalRPM < maxEngineRPM){
            gearIndex = 0;
            engineRPM = currentTotalRPM;
        }
        else {
            gearIndex = (int)((currentTotalRPM-maxEngineRPM)/(maxEngineRPM-minEngineRPMtoDownshift));
            engineRPM = minEngineRPMtoDownshift + ((currentTotalRPM-maxEngineRPM)%(maxEngineRPM - minEngineRPMtoDownshift));
        }
        currentAcelleration = maxAcceleration * acellerationMultiplierForEachGear[gearIndex];
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 collisionImpactDirection = -collision.contacts[0].normal;
        rb.AddForce(collisionImpactDirection * 10f, ForceMode.Impulse);
    }

    void ApplyCustomGravity()
    {
        Vector3 gravityForce = new Vector3(0, -customGravityScale * rb.mass, 0);
        rb.AddForce(gravityForce, ForceMode.Acceleration);
    }

    void CreateWheelEffects()
    {
        foreach (TrailRenderer trail in trails)
        {
            trail.emitting = applySkidMarks;
        }
    }
}

public class CarActionSpace
{
    // indexes go as follows 0: GoForward 1: GoBackwards 2: TurnRight 3: TurnLeft 4: Break
    private bool[] actions = new bool[4];

    public bool[] getActions() => actions;
    public void Reset(){
        for(int i = 0; i < actions.Length; i++){
            actions[i] = false;
        }
    }

    public void setAction(int index){
        actions[index] = true;
    }
}
