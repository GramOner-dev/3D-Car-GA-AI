using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour {

    public Transform car;
	public float distance = 6.4f;
	public float height = 1.4f;
	public float rotationDamping = 3.0f;
	public float heightDamping = 2.0f;
	public float zoomIncrease = 0.5f;
	public float defaultFOV = 60f;
    public float maxFOV = 110f;
    public float minimumMovementThreshold;

	private Vector3 rotationVector;

	void LateUpdate(){
		LookAtCar();
	}

	void FixedUpdate(){
		Vector3 localVelocity = car.InverseTransformDirection(car.GetComponent<Rigidbody>().velocity);
		if (localVelocity.z < -0.1f){
			Vector3 temp = rotationVector;
			temp.y = car.eulerAngles.y + 180;
			rotationVector = temp;
		}
		else{
			Vector3 temp = rotationVector;
			temp.y = car.eulerAngles.y;
			rotationVector = temp;
		}
		float acc = car.GetComponent<Rigidbody>().velocity.magnitude;
		GetComponent<Camera>().fieldOfView = Mathf.Clamp(defaultFOV + acc * zoomIncrease, defaultFOV, maxFOV);  
	}

    void LookAtCar(){
        float wantedAngle = rotationVector.y;
		float wantedHeight = car.position.y + height;
		float myAngle = transform.eulerAngles.y;
		float myHeight = transform.position.y;

		myAngle = Mathf.LerpAngle(myAngle, wantedAngle, rotationDamping*Time.deltaTime);
		myHeight = Mathf.Lerp(myHeight, wantedHeight, heightDamping*Time.deltaTime);

		Quaternion currentRotation = Quaternion.Euler(0, myAngle, 0);
		transform.position = car.position;
		transform.position -= currentRotation * Vector3.forward*distance;
		Vector3 temp = transform.position; //temporary variable so Unity doesn't complain
		temp.y = myHeight;
		transform.position = temp;
		transform.LookAt(car);
    }
}