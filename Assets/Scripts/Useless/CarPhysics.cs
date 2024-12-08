using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPhysics
{
    public static Vector3 CalculateDrag(float frontalArea, float dragConstant, Vector3 velocity) => -frontalArea * dragConstant * velocity * velocity.magnitude;
    public static Vector3 CalculateRollingResistance(float rollingResistanceConstant, Vector3 velocity) => -rollingResistanceConstant * velocity;
    public static Vector3 CalculateBreakingForce(Vector3 velocity, float frictionCoefficient) => (velocity / velocity.magnitude) * frictionCoefficient;
    public static Vector3 CalculateAcceleration(Vector3 netForce, float carMass) => netForce / carMass;
    public static Vector3 CalculateVelocity(Vector3 velocity, float timeIncrementBetweenPhysicsUpdates, Vector3 accelleration) => velocity + timeIncrementBetweenPhysicsUpdates * accelleration;
    
    public static float CalculateCorneringForce(float slipAngle, float corneringStiffness) =>  slipAngle * corneringStiffness;
    public static float CalculateWheelTraction(float torque, float wheelRadius) => torque / wheelRadius;
    public static float CalculateEngineTorque(float RPM) => 2 * Mathf.PI * RPM / 60;
    public static float CalculateWheelTorque(float engineTorque, float currentGearRation, float finalDriveRation) => engineTorque * currentGearRation * finalDriveRation;
    public static float CalculateTranslationalVelocity(float wheelRadius, float wheelTorque) => wheelRadius * wheelTorque;
    public static float CalculateRearWheelSlipAngle(float angularSpeed, float lateralVelocity, float longitudinalVelocity, float distanceFromCGToRearAxel) => Mathf.Atan((lateralVelocity - angularSpeed * distanceFromCGToRearAxel) / longitudinalVelocity);
    public static float CalculateFrontWheelSlipAngle(float angularSpeed, float lateralVelocity, float longitudinalVelocity, float distanceFromCGToFrontAxel, float steeringAngle)
    {
        int signumOfLongVel = 0;
        if (longitudinalVelocity > 0) signumOfLongVel = 1;
        if (longitudinalVelocity < 0) signumOfLongVel = -1;
        if(longitudinalVelocity == 0f) return 0f;
        return Mathf.Atan((lateralVelocity + angularSpeed * distanceFromCGToFrontAxel) / longitudinalVelocity) - steeringAngle * signumOfLongVel;
    }
    public static float CalculateLateralForce(float corneringStiffness, float slipAngle, float maxNormalizedFrictionForce, float load)
    {
        float lateralForce = corneringStiffness * slipAngle;
        if(Mathf.Abs(lateralForce) > maxNormalizedFrictionForce) lateralForce = lateralForce /  Mathf.Abs(lateralForce) * maxNormalizedFrictionForce;
        lateralForce *= load;
        lateralForce *= 0.5f;
        return lateralForce;
    }

    public static float CalculateWheelTurnSpeed(float speed, float wheelRadius) => speed / wheelRadius;
    public static float CalculateMaxNormalizedFrictionForce(float surfaceFrictionCoefficient, float weightDistributionRatio) => surfaceFrictionCoefficient * weightDistributionRatio;

}
