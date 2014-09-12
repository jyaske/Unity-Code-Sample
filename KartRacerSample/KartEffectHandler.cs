using UnityEngine;
using System.Collections;

public class KartEffectHandler : MonoBehaviour {

	/*=============================================================================================================================
	KartEffectsHandler.cs - JD Yaske
	This class takes in the base kart handling values and modifies them based on certain parameters. Any effects that change a 
	kart's speed or handling go into here. One of these classes is present on each kart.
	===============================================================================================================================*/


	KartControllerCustom controller;

	//These values are the "base" values for the kart. They are what the values return to during normal operation.
	#region InitialValuesForKart
	float straightLineSpeed;
	float turningSpeed;	
	float offTrackSpeed;
	float drift_turningSpeed;
	float featherDrift_turningSpeed;

	float maxTurnRate;
	float drift_maxTurnRate;
	float featherDrift_maxTurnRate;

	float straightLineAcceleration;
	float turnRateAcceleration;
	float drift_turnRateAcceleration;
	float featherDrift_turnRateAcceleration;

	float slowingDeceleration;
	float turningAntiAcceleration;
	float drift_turningAntiAcceleration;
	float featherDrift_turningAntiAcceleration;

	float offTrackDeceleration;
	float offTrackDecelerationOriginal;
	float offTrackDecelerationRate;

	float drivingForce;
	float groundDrag;
	float airDrag;
	float inAirTurningModifier;
	float drift_groundDrag;
	float featherDrift_groundDrag;
	float brakingDeceleration;
	float brakingSpeed;

	#endregion

	//These are the current, or "modified" values. These are the currently used values, that get affected by certain conditions.
	#region ModifiedKartValues
	float modifiedStraightLineSpeed;
	float modifiedTurningSpeed;	
	float modifiedOffTrackSpeed;
	float modifiedDrift_turningSpeed;
	float modifiedFeatherDrift_turningSpeed;
	
	float modifiedMaxTurnRate;
	float modifiedDrift_maxTurnRate;
	float modifiedFeatherDrift_maxTurnRate;
	
	float modifiedStraightLineAcceleration;
	float modifiedTurnRateAcceleration;
	float modifiedDrift_turnRateAcceleration;
	float modifiedFeatherDrift_turnRateAcceleration;
	
	float modifiedSlowingDeceleration;
	float modifiedTurningAntiAcceleration;
	float modifiedDrift_turningAntiAcceleration;
	float modifiedFeatherDrift_turningAntiAcceleration;
	#endregion

	#region InternalValues
	float currentSpeed;
	float currentTurnRate;

	bool speedModded = false;
	bool turnRateModded = false;
	#endregion

	//Do initial setup throught Unity Start function.
	void Start()
	{
		controller = GetComponent<KartControllerCustom>();
		SetInitialValues();
		ResetAll();
	}

	//Used to disable kart functionality for a given time.
	public void ApplyStun(float stunTime)
	{
		networkView.RPC("StunCar_RPC", RPCMode.All, stunTime);
	}

	//RPC function to disable kart functionality on both client and server.
	[RPC]
	void StunCar_RPC(float stunTime)
	{
		StartCoroutine(StunCar(stunTime));
	}
	
	//Stuns the car for a given period of time
	IEnumerator StunCar(float stunTime)
	{
		ModifyTopSpeed(-straightLineSpeed);
		ModifyTurningSpeed(-turningSpeed);
		ModifyDriftingSpeed(-drift_turningSpeed);
		ModifyDriftingFeatherSpeed(-featherDrift_turningSpeed);
		ModifyDriftDeceleration(10f);
		ModifyDriftFeatherDeceleration(10f);
		ModifyStraightLineDeceleration(10f);
		ModifyTurningDeceleration(10f);

		ModifyTurnRate(-maxTurnRate);
		ModifyDriftTurnRate(-drift_maxTurnRate);
		ModifyFeatherTurnRate(-featherDrift_maxTurnRate);

		yield return new WaitForSeconds(stunTime);
		ResetTopSpeed();
		ResetTurningSpeed();
		ResetDriftingSpeed();
		ResetDriftingFeatherSpeed();
		ResetDriftDeceleration();
		ResetDriftFeatherDeceleration();
		ResetStraightLineDeceleration();
		ResetTurningDeceleration();

		ResetTurnRate();
		ResetDriftTurnRate();
		ResetFeatherTurnRate();
	}
	
	//Apply an immediate speed boost to the car
	public void ApplySpeedBoost(float boostSpeed)
	{
		ModifyCurrentSpeed( boostSpeed );
	}

	//This function takes the current car rates from the controller, and applies modifications to them based on certain states
	public float[] CalculateRates(KartControllerCustom.DrivingState drivingState, float maxSpeed, float currentTurnRate, bool slowing, bool onTerrain, bool holdDrift)
	{
		float[] kartPhysicsValues = new float[2];

		if(speedModded)
			speedModded = false;
		else
			currentSpeed = maxSpeed;



		switch(drivingState)
		{
		case KartControllerCustom.DrivingState.forward:
			if(currentTurnRate < -modifiedTurnRateAcceleration*2)
				currentTurnRate += modifiedTurnRateAcceleration*2;
			else if(currentTurnRate > modifiedTurnRateAcceleration*2)
				currentTurnRate -= modifiedTurnRateAcceleration*2;
			else
				currentTurnRate = 0;
			
			if(currentSpeed < modifiedStraightLineSpeed && !slowing && !onTerrain)
			{
				currentSpeed += modifiedStraightLineAcceleration;
			}
			else if(currentSpeed > modifiedStraightLineSpeed)
			{
				currentSpeed -= modifiedSlowingDeceleration;
			}
			
			rigidbody.drag = groundDrag;
			break;
			
		case KartControllerCustom.DrivingState.left:
			if(currentTurnRate > 0)
			{
				currentTurnRate -= modifiedTurnRateAcceleration * 6;
			}
			else if(currentTurnRate > -modifiedMaxTurnRate)
			{
				currentTurnRate -= modifiedTurnRateAcceleration;
			}
			else if(currentTurnRate < -modifiedMaxTurnRate)
			{
				currentTurnRate += modifiedTurnRateAcceleration;
			}
			else
				currentTurnRate = -modifiedMaxTurnRate;
			
			if(currentSpeed > modifiedStraightLineSpeed - (modifiedStraightLineSpeed - modifiedTurningSpeed ) && !onTerrain)
			{
				currentSpeed -= modifiedTurningAntiAcceleration;
			}
			else if(currentSpeed < modifiedStraightLineSpeed - (modifiedStraightLineSpeed - modifiedTurningSpeed)  && !onTerrain)
				currentSpeed += modifiedStraightLineAcceleration;
			
			rigidbody.drag = groundDrag;
			break;
			
		case KartControllerCustom.DrivingState.right:
			if(currentTurnRate < 0)
			{
				currentTurnRate += modifiedTurnRateAcceleration * 6;
			}
			else if(currentTurnRate < modifiedMaxTurnRate)
				currentTurnRate += modifiedTurnRateAcceleration;
			else if(currentTurnRate > modifiedMaxTurnRate)
				currentTurnRate -= modifiedTurnRateAcceleration;
			else
				currentTurnRate = modifiedMaxTurnRate;
			
			if(currentSpeed > modifiedStraightLineSpeed - (modifiedStraightLineSpeed - modifiedTurningSpeed)  && !onTerrain)
			{
				currentSpeed -= modifiedTurningAntiAcceleration;
			}
			else if( currentSpeed < modifiedStraightLineSpeed - (modifiedStraightLineSpeed - modifiedTurningSpeed)  && !onTerrain)
				currentSpeed += modifiedStraightLineAcceleration;
			
			rigidbody.drag = groundDrag;
			break;
			
		case KartControllerCustom.DrivingState.driftLeft:
			if(!holdDrift)
			{
				if(currentTurnRate > 0)
				{
					currentTurnRate -= modifiedDrift_turnRateAcceleration * 3;
				}
				else if(currentTurnRate > -modifiedDrift_maxTurnRate)
				{
					currentTurnRate -= modifiedDrift_turnRateAcceleration;
				}
				else if(currentTurnRate < -modifiedDrift_maxTurnRate)
				{
					currentTurnRate += modifiedDrift_turnRateAcceleration;
				}
				else
					currentTurnRate = -modifiedDrift_maxTurnRate;
				
				if(currentSpeed > modifiedDrift_turningSpeed   && !onTerrain)
				{
					currentSpeed -= modifiedDrift_turningAntiAcceleration;
				}
				else if(currentSpeed < modifiedDrift_turningSpeed  && !onTerrain)
					currentSpeed += modifiedStraightLineAcceleration;
				
				rigidbody.drag = drift_groundDrag;
			}
			else if(holdDrift)
			{
				if(currentTurnRate > 0)
				{
					currentTurnRate -= modifiedFeatherDrift_turnRateAcceleration * 3;
				}
				else if(currentTurnRate > -modifiedFeatherDrift_maxTurnRate)
				{
					currentTurnRate -= modifiedFeatherDrift_turnRateAcceleration;
				}
				else if(currentTurnRate < -modifiedFeatherDrift_maxTurnRate)
				{
					currentTurnRate += modifiedFeatherDrift_turnRateAcceleration;
				}
				else
					currentTurnRate = -modifiedFeatherDrift_maxTurnRate;
				
				if(currentSpeed > modifiedFeatherDrift_turningSpeed  && !onTerrain)
				{
					currentSpeed -= modifiedFeatherDrift_turningAntiAcceleration;
				}
				else if(currentSpeed < modifiedFeatherDrift_turningSpeed  && !onTerrain)
					currentSpeed += modifiedStraightLineAcceleration;
				
				rigidbody.drag = featherDrift_groundDrag;
			}
			
			
			break;
			
		case KartControllerCustom.DrivingState.driftRight:
			if( !holdDrift )
			{
				
				if(currentTurnRate < 0)
				{
					currentTurnRate += modifiedDrift_turnRateAcceleration *3;
				}
				else if(currentTurnRate < modifiedDrift_maxTurnRate)
				{
					currentTurnRate += modifiedDrift_turnRateAcceleration;
				}
				else if(currentTurnRate > modifiedDrift_maxTurnRate)
				{
					currentTurnRate -= modifiedDrift_turnRateAcceleration;
				}
				else
					currentTurnRate = modifiedDrift_maxTurnRate;
				
				if(currentSpeed > modifiedDrift_turningSpeed  && !onTerrain)
				{
					currentSpeed -= modifiedDrift_turningAntiAcceleration;
				}
				else if( currentSpeed < modifiedDrift_turningSpeed  && !onTerrain)
					currentSpeed += modifiedStraightLineAcceleration;
				
				rigidbody.drag = drift_groundDrag;
			}
			else if(holdDrift)
			{
				if(currentTurnRate < 0)
				{
					currentTurnRate += modifiedFeatherDrift_turnRateAcceleration *3;
				}
				else if(currentTurnRate < modifiedFeatherDrift_maxTurnRate)
				{
					currentTurnRate += modifiedFeatherDrift_turnRateAcceleration;
				}
				else if(currentTurnRate > modifiedFeatherDrift_maxTurnRate)
				{
					currentTurnRate -= modifiedFeatherDrift_turnRateAcceleration;
				}
				else
					currentTurnRate = modifiedFeatherDrift_maxTurnRate;
				
				if(currentSpeed > modifiedFeatherDrift_turningSpeed  && !onTerrain)
				{
					currentSpeed -= modifiedFeatherDrift_turningAntiAcceleration;
				}
				else if( currentSpeed < modifiedFeatherDrift_turningSpeed  && !onTerrain)
					currentSpeed += modifiedStraightLineAcceleration;
				
				rigidbody.drag = featherDrift_groundDrag;
			}
			break;
			
		case KartControllerCustom.DrivingState.stopped:
			currentTurnRate = 0;
			currentSpeed = 0f;
			break;
			
		case KartControllerCustom.DrivingState.finished:
			break;
		}
		
		//apply slow for braking
		if(slowing && currentSpeed > brakingSpeed )
			currentSpeed -= brakingDeceleration;
		
		//apply slow for terrain
		if( onTerrain && currentSpeed > offTrackSpeed)
		{
			currentSpeed -= offTrackDeceleration;
			offTrackDeceleration += offTrackDecelerationRate;

		}
		else if(!onTerrain)
			offTrackDeceleration = offTrackDecelerationOriginal;

		kartPhysicsValues[0] = currentSpeed;
		kartPhysicsValues[1] = currentTurnRate;


		return kartPhysicsValues;

	}
	
	//Called initially, this function sets all of the internal values to the karts' base values, for reference later.
	public void SetInitialValues()
	{
		straightLineSpeed = controller.straightLineSpeed;
		turningSpeed = controller.turningSpeed;	
		offTrackSpeed = controller.offTrackSpeed;
		drift_turningSpeed = controller.drift_turningSpeed;
		featherDrift_turningSpeed = controller.featherDrift_turningSpeed;
		
		maxTurnRate = controller.maxTurnRate;
		drift_maxTurnRate = controller.drift_maxTurnRate;
		featherDrift_maxTurnRate = controller.featherDrift_maxTurnRate;
		
		straightLineAcceleration = controller.straightLineAcceleration;
		turnRateAcceleration = controller.turnRateAcceleration;
		drift_turnRateAcceleration = controller.drift_turnRateAcceleration;
		featherDrift_turnRateAcceleration = controller.featherDrift_turnRateAcceleration;
		
		slowingDeceleration = controller.slowingDeceleration;
		turningAntiAcceleration = controller.turningAntiAcceleration;
		drift_turningAntiAcceleration = controller.drift_turningAntiAcceleration;
		featherDrift_turningAntiAcceleration = controller.featherDrift_turningAntiAcceleration;

		brakingDeceleration = controller.brakingDeceleration;
		brakingSpeed = controller.brakingSpeed;

		offTrackDeceleration = controller.offTrackDeceleration;
		offTrackDecelerationOriginal = controller.offTrackDeceleration;
		offTrackDecelerationRate = controller.offTrackDecelFactor;

		drivingForce = controller.drivingForce;
		groundDrag = controller.groundDrag;
		airDrag = controller.airDrag;
		inAirTurningModifier = controller.inAirTurningModifier;
		drift_groundDrag = controller.drift_groundDrag;
		featherDrift_groundDrag = controller.featherDrift_groundDrag;
	}


//These functions are used to change each individual value by a certain differential
#region IndividualAttributeChangers

	#region SpeedChanges
	void ModifyTopSpeed(float diff)
	{
		modifiedStraightLineSpeed += diff;
	}

	void ModifyTurningSpeed(float diff)
	{
		modifiedTurningSpeed += diff;
	}

	void ModifyOffTrackSpeed(float diff)
	{
		modifiedOffTrackSpeed += diff;
	}

	void ModifyDriftingSpeed(float diff)
	{
		modifiedDrift_turningSpeed += diff;
	}

	void ModifyDriftingFeatherSpeed(float diff)
	{
		modifiedFeatherDrift_turningSpeed += diff;
	}
	#endregion

	#region MaxTurnRateChanges
	void ModifyTurnRate(float diff)
	{
		modifiedMaxTurnRate += diff;
	}

	void ModifyDriftTurnRate(float diff)
	{
		modifiedDrift_maxTurnRate += diff;
	}

	void ModifyFeatherTurnRate(float diff)
	{
		modifiedFeatherDrift_maxTurnRate += diff;
	}
	#endregion

	#region AccelerationChanges
	void ModifyStraightLineAcceleration(float diff)
	{
		modifiedStraightLineAcceleration += diff;
	}

	void ModifyTurningAcceleration(float diff)
	{
		modifiedTurnRateAcceleration += diff;
	}

	void ModifyDriftAcceleration(float diff)
	{
		modifiedDrift_turnRateAcceleration += diff;
	}

	void ModifyDriftFeatherAcceleration(float diff)
	{
		modifiedFeatherDrift_turnRateAcceleration += diff;
	}
	#endregion

	#region DecelerationChanges
	void ModifyStraightLineDeceleration(float diff)
	{
		modifiedSlowingDeceleration += diff;
	}

	void ModifyTurningDeceleration(float diff)
	{
		modifiedTurningAntiAcceleration += diff;
	}

	void ModifyDriftDeceleration(float diff)
	{
		modifiedDrift_turningAntiAcceleration += diff;
	}

	void ModifyDriftFeatherDeceleration(float diff)
	{
		modifiedFeatherDrift_turningAntiAcceleration += diff;
	}
	#endregion

	#region CurrentValueMods
	void ModifyCurrentSpeed(float diff)
	{
		currentSpeed += diff;
		speedModded = true;
	}
	#endregion

#endregion


//These functions are used to reset kart values back to their default values.
#region ResetAttributes
	void ResetAll()
	{
		ResetDrag();
		ResetDriftAcceleration();
		ResetDriftDeceleration();
		ResetDriftFeatherAcceleration();
		ResetDriftFeatherDeceleration();
		ResetDriftingFeatherSpeed();
		ResetDriftingSpeed();
		ResetDriftTurnRate();
		ResetDrivingForce();
		ResetFeatherTurnRate();
		ResetOffTrackSpeed();
		ResetStraightLineAcceleration();
		ResetStraightLineDeceleration();
		ResetTenacity();
		ResetTopSpeed();
		ResetTurningAcceleration();
		ResetTurningDeceleration();
		ResetTurningSpeed();
		ResetTurnRate();
	}

	#region SpeedChanges
	void ResetTopSpeed()
	{
		modifiedStraightLineSpeed = straightLineSpeed;
	}
	
	void ResetTurningSpeed()
	{
		modifiedTurningSpeed = turningSpeed;
	}
	
	void ResetOffTrackSpeed()
	{
		modifiedOffTrackSpeed = offTrackSpeed;
	}
	
	void ResetDriftingSpeed()
	{
		modifiedDrift_turningSpeed = drift_turningSpeed;
	}
	
	void ResetDriftingFeatherSpeed()
	{
		modifiedFeatherDrift_turningSpeed = featherDrift_turningSpeed;
	}
	#endregion
	
	#region MaxTurnRateChanges
	void ResetTurnRate()
	{
		modifiedMaxTurnRate = maxTurnRate;
	}
	
	void ResetDriftTurnRate()
	{
		modifiedDrift_maxTurnRate = drift_maxTurnRate;
	}
	
	void ResetFeatherTurnRate()
	{
		modifiedFeatherDrift_maxTurnRate = featherDrift_maxTurnRate;
	}
	#endregion
	
	#region AccelerationChanges
	void ResetStraightLineAcceleration()
	{
		modifiedStraightLineAcceleration = straightLineAcceleration;
	}
	
	void ResetTurningAcceleration()
	{
		modifiedTurnRateAcceleration = turnRateAcceleration;
	}
	
	void ResetDriftAcceleration()
	{
		modifiedDrift_turnRateAcceleration = drift_turnRateAcceleration;
	}
	
	void ResetDriftFeatherAcceleration()
	{
		modifiedFeatherDrift_turnRateAcceleration = featherDrift_turnRateAcceleration;
	}
	#endregion
	
	#region DecelerationChanges
	void ResetStraightLineDeceleration()
	{
		modifiedSlowingDeceleration = slowingDeceleration;
	}
	
	void ResetTurningDeceleration()
	{
		modifiedTurningAntiAcceleration = turningAntiAcceleration;
	}
	
	void ResetDriftDeceleration()
	{
		modifiedDrift_turningAntiAcceleration = drift_turningAntiAcceleration;
	}
	
	void ResetDriftFeatherDeceleration()
	{
		modifiedFeatherDrift_turningAntiAcceleration = featherDrift_turningAntiAcceleration;
	}
	#endregion

#endregion

}
