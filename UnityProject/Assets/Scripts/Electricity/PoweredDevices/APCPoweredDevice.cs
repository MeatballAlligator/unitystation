﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class APCPoweredDevice : NetworkBehaviour
{
	public float MinimumWorkingVoltage = 190;
	public float MaximumWorkingVoltage = 300;
	public bool IsEnvironmentalDevice = false;

	[SerializeField]
	private float wattusage = 0.01f;

	public float Wattusage
	{
		get { return wattusage; }
		set
		{
			wattusage = value;
			Resistance = 240 / (value / 240);
		}
	}

	public float Resistance = 99999999;
	public APC RelatedAPC;
	public IAPCPowered Powered;
	public bool AdvancedControlToScript;

	public bool StateUpdateOnClient = true;

	[SyncVar(hook = nameof(UpdateSynchronisedState))]
	public PowerStates State;

	private void Awake()
	{
		EnsureInit();
	}


	void Start()
	{
		Logger.LogTraceFormat("{0}({1}) starting, state {2}", Category.Electrical, name, transform.position.To2Int(), State);
		if (Wattusage > 0)
		{
			Resistance = 240 / (Wattusage / 240);
		}
		if (RelatedAPC != null)
		{
			if (IsEnvironmentalDevice)
			{
				RelatedAPC.EnvironmentalDevices.Add(this);
			}
			else {
				RelatedAPC.ConnectedDevices.Add(this);
			}
		}
	}

	private void EnsureInit()
	{
		if (Powered != null) return;
		Powered = GetComponent<IAPCPowered>();
	}

	public void SetAPC(APC _APC)
	{
		RemoveFromAPC();
		RelatedAPC = _APC;
		if (IsEnvironmentalDevice)
		{
			RelatedAPC.EnvironmentalDevices.Add(this);
		}
		else {
			RelatedAPC.ConnectedDevices.Add(this);
		}
	}

	public void RemoveFromAPC()
	{
		if (RelatedAPC != null)
		{
			if (IsEnvironmentalDevice)
			{
				if (RelatedAPC.EnvironmentalDevices.Contains(this))
				{
					RelatedAPC.EnvironmentalDevices.Remove(this);
				}

			}
			else {
				if (RelatedAPC.ConnectedDevices.Contains(this))
				{
					RelatedAPC.ConnectedDevices.Remove(this);
				}
			}
		}
	}

	public override void OnStartClient()
	{
		EnsureInit();
		UpdateSynchronisedState(State, State);
	}

	public override void OnStartServer()
	{
		EnsureInit();
		UpdateSynchronisedState(State, State);
	}

	public void APCBroadcastToDevice(APC APC)
	{
		if (RelatedAPC == null)
		{
			SetAPC(APC);
		}
	}
	public void PowerNetworkUpdate(float Voltage) //Could be optimised to not update when voltage is same as previous voltage
	{
		if (Powered != null)
		{
			if (AdvancedControlToScript)
			{
				Powered.PowerNetworkUpdate(Voltage);
			}
			else
			{
				var NewState = PowerStates.Off;
				if (Voltage <= 1)
				{
					NewState = PowerStates.Off;
				}
				else if (Voltage > MaximumWorkingVoltage)
				{
					NewState = PowerStates.OverVoltage;
				}
				else if (Voltage < MinimumWorkingVoltage)
				{
					NewState = PowerStates.LowVoltage;
				}
				else {
					NewState = PowerStates.On;
				}

				if (NewState != State)
				{
					State = NewState;
					Powered.StateUpdate(State);
				}
			}

		}
	}
	public void OnDisable()
	{
		RemoveFromAPC();
	}
	private void UpdateSynchronisedState(PowerStates _OldState, PowerStates _State)
	{
		EnsureInit();
		if (_State != State)
		{
			Logger.LogTraceFormat("{0}({1}) state changing {2} to {3}", Category.Electrical, name, transform.position.To2Int(), State, _State);
		}

		State = _State;
		if (Powered != null && StateUpdateOnClient)
		{
			Powered.StateUpdate(State);
		}
	}
}


public enum PowerStates
{
	Off,
	LowVoltage,
	On,
	OverVoltage,
}