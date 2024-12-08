using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    private BoxCollider[] checkPoints;
    public BoxCollider car;
    public int currentCheckPointIndex;
    public int totalCheckpoints = 0;
    public float carsDistanceToNextCheckpoint;
    public float distanceToNextCheckpoint;
    public float rotationRelativeToNextCheckpoint;
    public int nextCheckPointIndex = 0;
    private BoxCollider nextCheckPoint;

    void Start(){
        nextCheckPoint = checkPoints[0];
        currentCheckPointIndex = - 1;
    }

    void Update()
    {
        if (currentCheckPointIndex != -1)
        {
            nextCheckPointIndex = currentCheckPointIndex == checkPoints.Length - 1 ? 0 : currentCheckPointIndex + 1;
            nextCheckPoint = checkPoints[nextCheckPointIndex];
            carsDistanceToNextCheckpoint = (car.transform.position - nextCheckPoint.transform.position).magnitude;
            distanceToNextCheckpoint = (checkPoints[currentCheckPointIndex].transform.position - nextCheckPoint.transform.position).magnitude;
        } else {
            nextCheckPointIndex = 0;
            nextCheckPoint = checkPoints[nextCheckPointIndex];
            carsDistanceToNextCheckpoint = (car.transform.position - nextCheckPoint.transform.position).magnitude;
            distanceToNextCheckpoint = (nextCheckPoint.transform.position - checkPoints[checkPoints.Length - 1].transform.position).magnitude;
        }
        Vector3 dirToNextCheckpoint = (nextCheckPoint.transform.position - car.transform.position).normalized;
        dirToNextCheckpoint.y = 0;
        Vector3 carForward = (car.transform.forward).normalized;
        carForward.y = 0;

        rotationRelativeToNextCheckpoint = Vector3.SignedAngle(carForward, dirToNextCheckpoint, Vector3.up);
    }
    private void OnTriggerEnter(Collider collider)
    {

        for(int i = 0; i < checkPoints.Length; i++)
        {
            if (collider == checkPoints[i])
            {
                if(i < currentCheckPointIndex) return;
                currentCheckPointIndex = i;
                totalCheckpoints++;
            }
        }
    }

    public void setCheckPoints(BoxCollider[] checkPoints)
    {
        this.checkPoints = checkPoints;
    }

    public float getDistanceBetweenCheckPoints() => carsDistanceToNextCheckpoint;
    public float getCarsDistanceToNextCheckPoint() => distanceToNextCheckpoint;
    public float getRotationRelativeToNextCheckpoint() => rotationRelativeToNextCheckpoint;
    public int getTotalCheckpoints() => totalCheckpoints;
}
