using UnityEngine;
using System.Collections;

public class KartControllerCustom : MonoBehaviour {

	/*=============================================================================================================================
	KartControllerCustom.cs - JD Yaske
	This is a custom controller written for a multiplayer racing game in Unity. It is intended to function similar to a Mario Kart
	style racing game. The controller has functionality for both standard driving as well as drifting. The script exists on both
	the client and server side objects. It is used with an authoritative server model. Almost all values are exposed in the 
	editor so that designers can customize karts with different handling types. Each exposed value has a short description to
	aid designers. Base values produce a balanced handling kart.
	===============================================================================================================================*/

	//Exposed for designers. Only change these values in the inspector on the prefab, keep them the same here for reference later.

	//Maximum turn angle for standard turns.
	public float maxTurnRate = 1.5f;

	//How quickly the maxTurnRate is reached, and how quickly straight line driving is reached after the turn button is released. 
	public float turnRateAcceleration = .055f;

	//Maximum straight line speed attainable. Increasing this may require an increase in force.
	public float straightLineSpeed = 100f;

	//How quickly the straightLineSpeed is reached
	public float straightLineAcceleration = .75f;

	//How quickly the car slows to the turningSpeed
	public float turningAntiAcceleration = .5f;

	//The maximum velocity while turning
	public float turningSpeed = 50f;

	//Driving speed while holding the brake button
	public float offTrackSpeed = 20f;

	//Driving speed while braking
	public float brakingSpeed = 50f;

	//This determines how quickly the car will slow down while under braking
	public float brakingDeceleration = 1f;

	//This determines the slowing rate from speed boosts
	public float slowingDeceleration = 1f;

	//Determines the deceleration when off track
	public float offTrackDeceleration = 2f;

	//Below are the drifting values

	//Maximum turn angle for drifting turns.
	public float drift_maxTurnRate = 1.5f;
	
	//How quickly the drift maxTurnRate is reached.
	public float drift_turnRateAcceleration = .055f;

	//The minimum velocity while drifting
	public float drift_turningSpeed = 50f;

	//Drag applied while the car is on the ground. Resists driving force, and allows for less sliding in turns.
	public float drift_groundDrag = 2f;

	//Slowing rate for drift
	public float drift_turningAntiAcceleration = .5f;

	//These values are what is used when feathering drift:
	//Maximum turn angle for drifting turns.
	public float featherDrift_maxTurnRate = .4f;
	
	//How quickly the drift maxTurnRate is reached.
	public float featherDrift_turnRateAcceleration = .055f;
	
	//The minimum velocity while drifting
	public float featherDrift_turningSpeed = 90f;
	
	//Drag applied while the car is on the ground. Resists driving force, and allows for less sliding in turns.
	public float featherDrift_groundDrag = .5f;
	
	//Slowing rate for drift
	public float featherDrift_turningAntiAcceleration = .5f;

	//Controls how much the body of the vehicle will lean when turning
	public float turnTiltFactor = 0f; 
	
	//How quickly the gravity force acceleration will change
	public float downForceJerk = .1f;
	
	//Starting acceleration for the gravitational force.
	public float initialDownForceAcceleration = .75f;
	
	//Maximum gravitational force that can be applied
	public float downforceMax = 100f;
	
	//Force applied during in-air turning
	public float sideAirForce = 15f;
	
	//Modifier for visual turning calculations
	public float visualTurnModifier = 8f;
	
	//Be careful when changing the next values. ONLY change them if you need to, and know why you need to change it.
	
	//The relative forward force applied to the car while on the ground. Changing this will not increase acceleration or speed, use the above values to set that first.
	public float drivingForce = 400f;
	
	//Drag applied while the car is on the ground. Resists driving force, and allows for less sliding in turns.
	public float groundDrag = 2f;
	
	//Drag applied while in the air.
	public float airDrag = .05f;
	
	//Amount the current turning rate is slowed while in the air.
	public float inAirTurningModifier = 2.5f;
	
	float currentTurnRate = 0;
	bool drifting = false;
	bool holdDrift = false;
	int updatesReceived = 0;
	float downForce = 0f;
	float downForceAcceleration = 0;
	float targetTurnRate = 0f;
	float maxSpeed = 0f;
	float turnRatio = 0;

	Vector3 orientationHitNormal;

	public bool canCalculate = true;

	public enum DrivingState
	{
		stopped,
		forward,
		left,
		right,
		driftLeft,
		driftRight,
		lockedInAir,
		finished
	}

	public DrivingState drivingState = DrivingState.stopped;

	public Transform kartBody;
	public Transform cameraLocation;

	bool canLockToTrack = true;

	bool slowing = false;
	bool onTerrain = false;

	bool tryingToStartDrift = false;
	bool applyDrivingForce = false;

	KartEffectHandler effectsHandler;

	void Start()
	{
		effectsHandler = GetComponent<KartEffectHandler>();

		//controller = GetComponent<CharacterController>();
		drivingState = DrivingState.stopped;

		GameEvents.raceBegan += OnRaceBegan;
		GameEventsClient.raceBegan += OnRaceBegan;

	}

	//Called to set approprite states when resetting a kart
	public bool ResettingKart()
	{
		if (drivingState != DrivingState.stopped) 
		{
			drivingState = DrivingState.stopped;
			return true;
		} 
		else
			return false;
	}

	//Activate a disabled kart
	public void ActivateKart()
	{
		drivingState = DrivingState.forward;
	}

	//Called from the input handling to set the appropriate kart settings based on input
	public void ChangeMoveState( bool turnRight, bool turnLeft, bool slowingDown, bool pressedSlow, bool doubleTap )
	{
		StartCoroutine(ChangeMoveStateCoroutine(turnRight, turnLeft, slowingDown, pressedSlow, doubleTap));
	}
	
	
	IEnumerator ChangeMoveStateCoroutine( bool turnRight, bool turnLeft, bool slowingDown, bool pressedSlow, bool doubleTap )
	{
		if(Network.isClient)
			yield return new WaitForSeconds(.1f);


		if(drivingState != DrivingState.stopped )
		{
			if(slowingDown && !drifting)
				slowing = true;
			else
				slowing = false;

			if(turnRight && !turnLeft)
			{
				if(!pressedSlow)
				{
					if(drivingState != DrivingState.driftRight)
					{
						drivingState = DrivingState.right;
						drifting = false;
						holdDrift = false;
					}
					else
					{
						drifting = true;
						holdDrift = false;
					}
				}
				else
				{
					drivingState = DrivingState.driftRight;
					drifting = true;
					slowing = false;
				}
			}
			else if ( turnLeft && !turnRight )
			{
				if(!pressedSlow)
				{
					if(drivingState != DrivingState.driftLeft)
					{
						drivingState = DrivingState.left;
						drifting = false;
						holdDrift = false;
					}
					else
					{
						drifting = true;
						holdDrift = false;
					}
				}
				else
				{
					drivingState = DrivingState.driftLeft;
					drifting = true;
					slowing = false;
				}
			}
			else if ( turnLeft && turnRight )
			{
				if(drifting)
				{
					if(drivingState == DrivingState.driftLeft || drivingState == DrivingState.driftRight)
					{
						holdDrift = true;
					}
				}
			}
			else
			{
				drivingState = DrivingState.forward;
				drifting = false;
				holdDrift = false;
			}
		}
	}

	//Disable kart after race is finished
	public void KartFinishedRace()
	{
		drivingState = DrivingState.stopped;
	}

	//This passes in the current kart turning and speed values to the KartEffectHandler, which modifies them based on certain conditions such as stuns etc.
	void CalculateCarRates()
	{
		float[] kartVals = effectsHandler.CalculateRates(drivingState, maxSpeed, currentTurnRate, slowing, onTerrain, holdDrift);

		maxSpeed = kartVals[0];
		currentTurnRate = kartVals[1];
	}

	//This function takes the calculated movement and input values and calculates the movement of the kart
	void CalculateServerMovement()
	{
		CalculateOrientation();
		
		float actualTurnRate = currentTurnRate;
		
		RaycastHit hit;
		//Calculations for on track movement
		if(Physics.Raycast(transform.position + transform.up*2, -transform.up, out hit, 4.5f, 17 << 8 ) && canLockToTrack)
		{
			#region NormalDriving

			if(hit.transform.gameObject.layer == LayerMask.NameToLayer("Grass"))
			{
				//On grass
				onTerrain = true;
			}
			else
			{
				onTerrain = false;
			}

			applyDrivingForce = true;

			downForce = 0f;
			downForceAcceleration = initialDownForceAcceleration;
			
			rigidbody.freezeRotation = true;
			
			Vector3 newPosition = hit.point + hit.normal;
			
			transform.position =newPosition;
			
			rigidbody.drag = groundDrag;
			#endregion
		}
		//Calculations for in air movement.
		else
		{
			#region OffTrack
			drifting = false;

			applyDrivingForce = false;
			
			actualTurnRate /= inAirTurningModifier;

			if(actualTurnRate < 0)
			{
				rigidbody.AddRelativeForce(-sideAirForce,0,0);
			}
			else if(actualTurnRate > 0)
			{
				rigidbody.AddRelativeForce(sideAirForce,0,0);
			}
			
			rigidbody.drag = airDrag;
			rigidbody.freezeRotation = true;
			
			rigidbody.AddRelativeForce(new Vector3(0,30f,0));
			
			//If we find a collider to lock orientation to
			if (Physics.Raycast(transform.position, -transform.up, out hit, 35f, 1<<9)) 
			{
				transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Vector3.Cross(transform.right, hit.normal), hit.normal), Time.deltaTime * 5.0f);
				
				rigidbody.AddRelativeForce(new Vector3(0,-downForce,0));
			}
			else
			{
				float wantedRotationX = Mathf.LerpAngle(transform.eulerAngles.x, 0, 2.5f * Time.deltaTime);
				float wantedRotationZ = Mathf.LerpAngle(transform.eulerAngles.z, 0, 2.5f * Time.deltaTime);
				
				transform.rotation = Quaternion.Euler(wantedRotationX, transform.eulerAngles.y, wantedRotationZ);
				
				rigidbody.AddForce(new Vector3(0,-downForce,0));
			}

			if(downForce < downforceMax)
			{
				downForce += downForceAcceleration;
				downForceAcceleration += downForceJerk;

			}
			#endregion
		}

		if(canCalculate)
			transform.Rotate(new Vector3(0, actualTurnRate, 0));
		
		if(rigidbody.velocity.magnitude < maxSpeed && drivingState != DrivingState.stopped && applyDrivingForce && canCalculate)
		{
			rigidbody.AddRelativeForce(new Vector3(0,0,drivingForce));
		}
	}

	//Fixed Update runs on a fixed interval, and triggers the movement calculations
	void FixedUpdate()
	{
		CalculateCarRates();
		CalculateServerMovement(); 
	}

	//This function locks the kart to the track, and moves it to the appropriate height above the road.
	void CalculateOrientation()
	{
		RaycastHit hit;

		if (Physics.Raycast(transform.position, -transform.up, out hit, 3f, 17 << 8) && canLockToTrack) {

			orientationHitNormal = hit.normal;

			transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, hit.normal), hit.normal);
		}
	}

	//Handles kart activation when the race is started.
	void OnRaceBegan()
	{
		ActivateKart();
	}

	//Used to manually unlock the kart from the track.
	public void Unlock(float duration = 1f)
	{
		canLockToTrack = false;
		
		StartCoroutine(LockToTrackAfterDuration(duration));
	}

	//Used if we want to use a custom jump angle and velocity, as opposed to using physics
	public void CustomJump(Transform align, Vector3 velocityToSet)
	{
		transform.rotation = align.rotation;
		rigidbody.velocity = velocityToSet;
	}

	//Triggering this enumerator will lock the kart back to the road after a given duration
	IEnumerator LockToTrackAfterDuration(float duration)
	{
		yield return new WaitForSeconds(duration);
		canLockToTrack = true;
	}

	/*Synchronizes the movement data between the client and the server. The server calculates the actual movement and sends the data back, which the
	client uses to visualize the movement*/
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) 
	{
		Vector3 pos = Vector3.zero;
		Quaternion rot = Quaternion.identity;
		float ctr = 0f;
		float currSpeed =0;
		bool isDrift = false;
		
		if (stream.isWriting) {
			pos = transform.position;
			rot = transform.rotation;
			ctr = currentTurnRate;
			isDrift = drifting;
		
			stream.Serialize(ref rot);
			stream.Serialize(ref pos);
			stream.Serialize(ref ctr);
			stream.Serialize(ref isDrift);

			updatesReceived++;

		} else {
			stream.Serialize(ref rot);
			stream.Serialize(ref pos);
			stream.Serialize(ref ctr);
			stream.Serialize(ref isDrift);

			updatesReceived++;

			GetComponent<KartVisualsCalculator>().SetKartData(pos, rot, ctr, isDrift);
		}
	}
}
	
