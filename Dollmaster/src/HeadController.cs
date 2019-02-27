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

        FreeControllerV3 headControl;

        FreeControllerV3 chestControl;
        DAZBone neckBone;

        float lastTriggered = 0;

        bool headLookingAtPlayer = false;

        Vector3 lookOffset = Vector3.zero;

        JSONStorable eyesStorable;

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
            neckBone = atom.GetStorableByID("neck") as DAZBone;

            alwaysLookAtMe = new JSONStorableBool("alwaysLookAtMe", false);
            dm.RegisterBool(alwaysLookAtMe);
            UIDynamicToggle forceLookToggle = ui.CreateToggle("Hold Gaze", 180, 40);
            alwaysLookAtMe.toggle = forceLookToggle.toggle;

            forceLookToggle.transform.Translate(0.415f, 0.01f, 0, Space.Self);
            forceLookToggle.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            forceLookToggle.labelText.color = new Color(1, 1, 1);

            eyesStorable = atom.GetStorableByID("Eyes");

            dm.CreateSpacer(true);
        }

        public override void Update()
        {
            base.Update();

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
                float behindAmount = 100f;
                Vector3 up = neckBone.transform.position - chestControl.transform.position;

                Vector3 targetOrigin = CameraTarget.centerTarget.transform.position + lookOffset;
                Vector3 delta = targetOrigin - headControl.transform.position;
                Vector3 target = CameraTarget.centerTarget.transform.position + delta.normalized * behindAmount;

                Quaternion lookRotation = Quaternion.LookRotation(delta, up);
                headControl.transform.rotation = Quaternion.Lerp(headControl.transform.rotation, lookRotation, Time.deltaTime * headTurnSpeed.val);
            }

            if (alwaysLookAtMe.val)
            {
                eyesStorable.SetStringChooserParamValue("lookMode", "Player");
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
                lookOffset.y = UnityEngine.Random.Range(-0.4f, 0.05f);
            }

        }
    }
}
