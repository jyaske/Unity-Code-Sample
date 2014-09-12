using UnityEngine;
using System.Collections;

public class KartVisualsCalculator : MonoBehaviour {
	
	/*=============================================================================================================================
	KartVisualsCalculator.cs - JD Yaske
	The KartVisualsCalculator is responsible for calculating the karts movement on the client side. All of the movement is controlled
	server side and sent to the client, but to decrease network data traffic, the positions are not synchronized every frame. To fill 
	in the gaps, the client	runs a simulation in between synchronizations. This class recieves the updated positions from the server 
	and interpolates the kart position from the current simulated position on the client, to the updated actual position recieved 
	from the server based on how long it has been since the last position sync. This allows the client to simulate smooth and natural 
	looking movement over a variety of connection speeds.
	===============================================================================================================================*/
	
	Transform kartBody;

	float lastSyncTime = 0f;
	float syncDelay = 0f;
	float syncTime = 0f;
	public bool useAnimations=false;

	float totalSync = 0f;
	float averageSyncTime = 0f;

	Vector3 carStartPos = Vector3.zero;
	Vector3 carEndPos = Vector3.zero;

	Vector3 velocity = Vector3.zero;
	Vector3 prevVelocity = Vector3.zero;

	Quaternion carStartRot = Quaternion.identity;
	Quaternion carEndRot = Quaternion.identity;

	Vector3 extrapolatedDirection = Vector3.zero;

	float currentLerpInterval = 0f;

	float angleToTurnFrom = 0f;
	float angleToTurnTo = 0f;
	KartControllerCustom controller;

	public bool isMine = false;

	float currentAngle = 0;

	int updateCount = 0;

	int driftVisual = 1;
	bool isDrifting = false;
	public bool IsDrifting { get { return isDrifting; } }

	bool applyDrivingForce = false;
	public bool ApplyDrivingForce { get { return applyDrivingForce; } } 

	public float TurningAngle
	{
		get { return angleToTurnFrom; }
	}

	//The start function finds the approprite components on the current gameobject
	void Start()
	{
		kartBody = gameObject.GetComponent<KartControllerCustom>().kartBody;
		controller = GetComponent<KartControllerCustom>();
	}

	//This is called from KartControllerCustom whenever sync data is recieved from the server
	public void SetKartData(Vector3 pos, Quaternion rot, float turningAngle, bool isDrift)
	{
		if(Network.isClient)
		{
			updateCount++;
			totalSync += syncTime;
			averageSyncTime = totalSync / updateCount;
			this.isDrifting = isDrift;

			if(isDrift)
				driftVisual = 2;
			else
				driftVisual = 1;

			turningAngle = driftVisual * turningAngle;
			syncTime = 0;

			angleToTurnFrom = currentAngle;

			syncDelay = Time.time - lastSyncTime;

			lastSyncTime = Time.time;

			carStartPos = transform.position;
			carStartRot = transform.rotation;

			carEndPos = pos;
			carEndRot = rot;
		
			currentLerpInterval = 0;
			angleToTurnTo = turningAngle;
		}
	}

	//Update is used to lerp the kart to the new interpolated position
	void Update()
	{
		if(Network.isClient)
		{
			syncTime += Time.deltaTime;
		
			if(syncTime / syncDelay <= 1)
			{
				transform.position = Vector3.Lerp(carStartPos, carEndPos, syncTime / syncDelay );
				transform.rotation = Quaternion.Slerp(carStartRot, carEndRot, syncTime / syncDelay);
				controller.canCalculate = false;
			}
			else
				controller.canCalculate = true;

		
			float lerpedAngle = Mathf.LerpAngle( angleToTurnFrom, angleToTurnTo, syncTime / syncDelay);
			currentAngle = lerpedAngle;

			kartBody.localEulerAngles = new Vector3(0, lerpedAngle * controller.visualTurnModifier, lerpedAngle * controller.turnTiltFactor * -1f);

		}
	}
}
