using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    public string targetTag = "Wall";       
    public Transform leftWallChecker, rightWallChecker, forwardWallChecker;
    public float distanceToLeftWall, distanceToRightWall, distanceToForwardWall;
    public float raycastDistance = 10f;
    public bool wasWallHit;

    private void Update() {
        distanceToLeftWall = CalculateDistance(leftWallChecker);
        distanceToRightWall = CalculateDistance(rightWallChecker);
        distanceToForwardWall = CalculateDistance(forwardWallChecker);
    }
    public float CalculateDistance(Transform wallChecker)
    {
        Vector3 rayOrigin = wallChecker.position;
        Vector3 rayDirection = wallChecker.forward;
        float distance = raycastDistance;
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, raycastDistance))
        {
            if (hit.collider.CompareTag(targetTag))
            {
                distance = Vector3.Distance(rayOrigin, hit.point);
            }
        }
        return distance;
    }

    public float getDistanceToLeftWall() => distanceToLeftWall;
    public float getDistanceToRightWall() => distanceToRightWall;
    public float getDistanceToForwardWall() => distanceToForwardWall;
    public float getCarViewDistanceToWall() => raycastDistance;
    public bool WasWallHit() => wasWallHit;


    void OnCollisionEnter(Collision collision)
    {
        if(!wasWallHit) {
            wasWallHit = collision.gameObject.tag == "Wall";
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach(Transform wallChecker in new Transform[] {leftWallChecker, rightWallChecker, forwardWallChecker}){
            Gizmos.DrawRay(wallChecker.position, wallChecker.forward * raycastDistance);
        }
    }
}
