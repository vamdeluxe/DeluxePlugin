using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin {

	// this class is a bit like the built-in CycleForceProducer but can only attach to rigidbodies within the containing atom
	// and force/torque is relative to the body itself by choosing apply axis rather than world axis
	public class CycleForce : MVRScript {

		protected Rigidbody RB;
		protected void SyncReceiver(string receiver) {
			if (receiver != null) {
				ForceReceiver fr;
				if (receiverNameToForceReceiver.TryGetValue(receiver, out fr)) {
					RB = fr.GetComponent<Rigidbody>();
				} else {
					RB = null;
				}
			} else {
				RB = null;
			}
		}
		protected JSONStorableStringChooser receiverChoiceJSON;

		protected ForceProducerV2.AxisName _forceAxis = ForceProducerV2.AxisName.Z;
		protected void SyncForceAxis(string axisName) {
			try {
				ForceProducerV2.AxisName an = (ForceProducerV2.AxisName)System.Enum.Parse(typeof(ForceProducerV2.AxisName), axisName);
				_forceAxis = an;
			}
			catch (System.ArgumentException) {
				Debug.LogError("Attempt to set axis to " + axisName + " which is not a valid axis name");
			}
		}
		protected JSONStorableStringChooser forceAxisJSON;


		protected ForceProducerV2.AxisName _torqueAxis = ForceProducerV2.AxisName.X;
		protected void SyncTorqueAxis(string axisName) {
			try {
				ForceProducerV2.AxisName an = (ForceProducerV2.AxisName)System.Enum.Parse(typeof(ForceProducerV2.AxisName), axisName);
				_torqueAxis = an;
			}
			catch (System.ArgumentException) {
				Debug.LogError("Attempt to set axis to " + axisName + " which is not a valid axis name");
			}
		}
		protected JSONStorableStringChooser torqueAxisJSON;

		protected JSONStorableFloat periodJSON;
		protected JSONStorableFloat periodRatioJSON;
		protected JSONStorableFloat forceDurationJSON;
		protected JSONStorableFloat forceFactorJSON;
		protected JSONStorableFloat forceQuicknessJSON;
		protected JSONStorableFloat torqueFactorJSON;
		protected JSONStorableFloat torqueQuicknessJSON;
		protected JSONStorableBool applyForceOnReturnJSON;

		protected List<string> receiverChoices;
		protected Dictionary<string, ForceReceiver> receiverNameToForceReceiver;

		public override void Init() {
			try {
				receiverChoices = new List<string>();
				receiverNameToForceReceiver = new Dictionary<string, ForceReceiver>();
				foreach (ForceReceiver fr in containingAtom.forceReceivers) {
					receiverChoices.Add(fr.name);
					receiverNameToForceReceiver.Add(fr.name, fr);
				}
				receiverChoiceJSON = new JSONStorableStringChooser("receiver", receiverChoices, null, "Receiver", SyncReceiver);
				receiverChoiceJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterStringChooser(receiverChoiceJSON);
				UIDynamicPopup dp = CreateScrollablePopup(receiverChoiceJSON);
				dp.popupPanelHeight = 1100f;
				dp.popup.alwaysOpen = true;

				string[] axisChoices = System.Enum.GetNames(typeof(ForceProducerV2.AxisName));
				List<string> axisChoicesList = new List<string>(axisChoices);

				forceAxisJSON = new JSONStorableStringChooser("forceAxis", axisChoicesList, _forceAxis.ToString(), "Force Axis", SyncForceAxis);
				forceAxisJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterStringChooser(forceAxisJSON);
				CreatePopup(forceAxisJSON,true);

				torqueAxisJSON = new JSONStorableStringChooser("torqueAxis", axisChoicesList, _torqueAxis.ToString(), "Torque Axis", SyncTorqueAxis);
				torqueAxisJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterStringChooser(torqueAxisJSON);
				CreatePopup(torqueAxisJSON,true);

				periodJSON = new JSONStorableFloat("period", 0.5f, 0f, 10f, false);
				periodJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(periodJSON);
				CreateSlider(periodJSON, true);

				periodRatioJSON = new JSONStorableFloat("periodRatio", 0.5f, 0f, 1f, true);
				periodRatioJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(periodRatioJSON);
				CreateSlider(periodRatioJSON, true);

				forceDurationJSON = new JSONStorableFloat("forceDuration", 1f, 0f, 1f, true);
				forceDurationJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(forceDurationJSON);
				CreateSlider(forceDurationJSON, true);

				forceFactorJSON = new JSONStorableFloat("forceFactor", 0f, 0f, 1000f, false, true);
				forceFactorJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(forceFactorJSON);
				CreateSlider(forceFactorJSON, true);

				forceQuicknessJSON = new JSONStorableFloat("forceQuickness", 10f, 0f, 50f, false, true);
				forceQuicknessJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(forceQuicknessJSON);
				CreateSlider(forceQuicknessJSON, true);

				torqueFactorJSON = new JSONStorableFloat("torqueFactor", 5f, 0f, 100f, false, true);
				torqueFactorJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(torqueFactorJSON);
				CreateSlider(torqueFactorJSON, true);

				torqueQuicknessJSON = new JSONStorableFloat("torqueQuickness", 10f, 0f, 50f, false, true);
				torqueQuicknessJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(torqueQuicknessJSON);
				CreateSlider(torqueQuicknessJSON, true);

				applyForceOnReturnJSON = new JSONStorableBool("applyForceOnReturn", true);
				applyForceOnReturnJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterBool(applyForceOnReturnJSON);
				CreateToggle(applyForceOnReturnJSON, true);
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		protected float timer;
		protected float forceTimer;
		protected float flip;
		protected Vector3 targetForce;
		protected Vector3 targetTorque;
		protected Vector3 currentForce;
		protected Vector3 currentTorque;

		protected void Start() {
			timer = 0f;
			forceTimer = 0f;
			flip = 1f;
		}

		protected virtual Vector3 AxisToVector(ForceProducerV2.AxisName axis) {
			Vector3 result;
			if (RB != null) {
				switch (axis) {
					case ForceProducerV2.AxisName.X:
						result = RB.transform.right;
						break;
					case ForceProducerV2.AxisName.NegX:
						result = -RB.transform.right;
						break;
					case ForceProducerV2.AxisName.Y:
						result = RB.transform.up;
						break;
					case ForceProducerV2.AxisName.NegY:
						result = -RB.transform.up;
						break;
					case ForceProducerV2.AxisName.Z:
						result = RB.transform.forward;
						break;
					case ForceProducerV2.AxisName.NegZ:
						result = -RB.transform.forward;
						break;
					default:
						result = Vector3.zero;
						break;
				}
			} else {
				result = Vector3.zero;
			}
			return (result);
		}

		protected void SetTargets(float percent) {
			targetForce = AxisToVector(_forceAxis) * percent * forceFactorJSON.val;
			targetTorque = AxisToVector(_torqueAxis) * percent * torqueFactorJSON.val;
		}

		// Use Update for the timers since this can account for time scale
		protected void Update() {
			timer -= Time.deltaTime;
			forceTimer -= Time.deltaTime;
			if (timer < 0.0f) {
				if ((flip > 0f && periodRatioJSON.val != 1f) || periodRatioJSON.val == 0f) {
					if (applyForceOnReturnJSON.val) {
						flip = -1f;
					} else {
						flip = 0f;
					}
					timer = periodJSON.val * (1f - periodRatioJSON.val);
					forceTimer = forceDurationJSON.val * periodJSON.val;
				} else {
					flip = 1f;
					timer = periodJSON.val * periodRatioJSON.val;
					forceTimer = forceDurationJSON.val * periodJSON.val;
				}
				SetTargets(flip);
			} else if (forceTimer < 0.0f) {
				SetTargets(0f);
			}
		}

		// FixedUpdate is called with each physics simulation frame by Unity
		void FixedUpdate() {
			try {
				// apply forces here
				float timeFactor = Time.fixedDeltaTime;
				currentForce = Vector3.Lerp(currentForce, targetForce, timeFactor * forceQuicknessJSON.val);
				currentTorque = Vector3.Lerp(currentTorque, targetTorque, timeFactor * torqueQuicknessJSON.val);
				if (RB && (!SuperController.singleton || !SuperController.singleton.freezeAnimation)) {
					RB.AddForce(currentForce, ForceMode.Force);
					RB.AddTorque(currentTorque, ForceMode.Force);
				}
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

	}
}