using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;



namespace DeluxePlugin.Dollmaster
{
    public class HeadController : BaseModule
    {
        JSONStorableBool alwaysLookAtMe;
        JSONStorableBool headControlEnabled;
        JSONStorableFloat durationBetweenLookChange;
        JSONStorableFloat headTurnSpeed;
        JSONStorableBool saccadeEnabled;

        FreeControllerV3 headControl;

        FreeControllerV3 chestControl;
        DAZBone chestBone;
        DAZBone neckBone;
        DAZBone hipBone;

        float lastTriggered = 0;

        bool headLookingAtPlayer = false;

        Vector3 lookOffset = Vector3.zero;

        EyesControl eyesControl;

        GameObject dummy;

        float nextSaccadeTime = Time.time;
        Vector3 saccadeOffset = new Vector3();

        public HeadController(DollmasterPlugin dm) : base(dm)
        {
            headControlEnabled = new JSONStorableBool("headControlEnabled", true);
            dm.RegisterBool(headControlEnabled);
            UIDynamicToggle moduleEnableToggle = dm.CreateToggle(headControlEnabled);
            moduleEnableToggle.label = "Enable Head Gaze";
            moduleEnableToggle.backgroundColor = Color.green;

            durationBetweenLookChange = new JSONStorableFloat("duration between look at toggle", 8, 1, 30, false);
            dm.RegisterFloat(durationBetweenLookChange);
            dm.CreateSlider(durationBetweenLookChange);

            headTurnSpeed = new JSONStorableFloat("headTurnSpeed", 2.5f, 0.01f, 20.0f);
            dm.RegisterFloat(headTurnSpeed);
            dm.CreateSlider(headTurnSpeed);

            headControl = atom.GetStorableByID("headControl") as FreeControllerV3;
            chestControl = atom.GetStorableByID("chestControl") as FreeControllerV3;
            chestBone = atom.GetStorableByID("chest") as DAZBone;
            neckBone = atom.GetStorableByID("neck") as DAZBone;
            hipBone = atom.GetStorableByID("hip") as DAZBone;

            alwaysLookAtMe = new JSONStorableBool("alwaysLookAtMe", false);
            dm.RegisterBool(alwaysLookAtMe);
            UIDynamicToggle forceLookToggle = ui.CreateToggle("Hold Gaze", 180, 40);
            alwaysLookAtMe.toggle = forceLookToggle.toggle;

            forceLookToggle.transform.Translate(0.415f, 0.01f, 0, Space.Self);
            forceLookToggle.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            forceLookToggle.labelText.color = new Color(1, 1, 1);

            eyesControl = atom.GetStorableByID("Eyes") as EyesControl;

            dm.CreateSpacer(true);

            saccadeEnabled = new JSONStorableBool("saccadeEnabled", true);
            dm.RegisterBool(saccadeEnabled);
            dm.CreateToggle(saccadeEnabled);

            dummy = new GameObject();
        }

        public override void Update()
        {
            base.Update();

            Vector3 targetOrigin = CameraTarget.centerTarget.transform.position + lookOffset;
            float behindAmount = 100f;
            Vector3 delta = targetOrigin - headControl.transform.position;
            Vector3 target = CameraTarget.centerTarget.transform.position + delta.normalized * behindAmount;
            target += Vector3.down * 0.2f;

            if (alwaysLookAtMe.val)
            {
                eyesControl.currentLookMode = EyesControl.LookMode.Target;
                eyesControl.lookAt = dummy.transform;
            }

            dummy.transform.position = CameraTarget.centerTarget.transform.position + saccadeOffset;

            if (saccadeEnabled.val)
            {
                if (Time.time >= nextSaccadeTime)
                {
                    saccadeOffset = UnityEngine.Random.insideUnitSphere * 0.1f;
                    nextSaccadeTime = Time.time + UnityEngine.Random.Range(0.5f, 1.5f);
                }
            }
            else
            {
                saccadeOffset = Vector3.zero;
            }



            if (headControlEnabled.val == false && alwaysLookAtMe.val == false)
            {
                return;
            }

            bool headSelected = headControl.linkToRB != null && (headControl.linkToRB.name == "MouseGrab" || headControl.linkToRB.name == "LeftHand" || headControl.linkToRB.name == "RightHand");
            if (headSelected)
            {
                lastTriggered = Time.time;
                return;
            }

            if ((headLookingAtPlayer && dm.arousal.value > 0) || alwaysLookAtMe.val)
            {


                Vector3 hipToNeck = Vector3.Normalize(neckBone.transform.position - hipBone.transform.position);
                Vector3 cameraUp = CameraTarget.centerTarget.transform.up;
                Vector3 hybridUp = Vector3.Lerp(cameraUp, hipToNeck, 0.5f);
                float dotProduct = Vector3.Dot(CameraTarget.centerTarget.transform.up, hipToNeck);

                //if (dotProduct >= -0.3f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(delta, hybridUp);
                    headControl.transform.rotation = Quaternion.RotateTowards(headControl.transform.rotation, lookRotation, headTurnSpeed.val * Time.deltaTime * 20);
                }
                //Vector3 headEuler = headControl.transform.localEulerAngles;
                //float headYaw = headEuler.z;
                //Debug.Log(headYaw);
                //headYaw = Mathf.Clamp(headYaw, 180 - 20, 180 + 20);
                //headEuler.z = headYaw;
                //headControl.transform.localEulerAngles = headEuler;
            }

        }

        public void Trigger()
        {
            if ((Time.time - lastTriggered) < durationBetweenLookChange.val)
            {
                return;
            }

            lastTriggered = Time.time;

            if (UnityEngine.Random.Range(0f, 100f) > 50f)
            {
                headLookingAtPlayer = false;
            }
            else
            {
                headLookingAtPlayer = true;
            }

            lookOffset = Vector3.zero;
            if (UnityEngine.Random.Range(0f, 100f) > 50f)
            {
                lookOffset = Vector3.up * UnityEngine.Random.Range(-0.6f, 0.05f);
            }

        }

        public override void OnDestroy()
        {
            if (dummy != null)
            {
                GameObject.Destroy(dummy);
            }
        }
    }
}
