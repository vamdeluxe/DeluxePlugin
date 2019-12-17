using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class PoseTransform
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    public class Pose
    {
        public Dictionary<string, PoseTransform> transforms = new Dictionary<string, PoseTransform>();
        public Pose(Atom atom, JSONNode atomJSON)
        {
            var obj = atomJSON.AsObject;
            var storables = obj["storables"].AsArray;
            Dictionary<string, JSONClass> storablesDict = new Dictionary<string, JSONClass>();
            storables.Childs.ToList().ForEach(node =>
            {
                var asClass = node.AsObject;
                storablesDict[asClass["id"]] = node.AsObject;
            });

            atom.freeControllers.ToList().ForEach(controller =>
            {
                string id = controller.storeId;
                if (id == "control")
                {
                    return;
                }

                if (storablesDict.ContainsKey(id) == false)
                {
                    return;
                }
                var storable = storablesDict[id];
                Vector3 position = GetLocalPosition(storable);
                Quaternion rotation = GetLocalRotation(storable);
                PoseTransform pt = new PoseTransform() { localPosition = position, localRotation = rotation };
                transforms[id] = pt;
            });
        }

        public Pose(Atom atom)
        {
            atom.freeControllers.ToList().ForEach(controller =>
            {
                transforms[controller.storeId] = new PoseTransform() { localPosition = controller.transform.localPosition, localRotation = controller.transform.localRotation };
            });
        }

        private Vector3 GetLocalPosition(JSONNode jn)
        {
            Vector3 v = new Vector3();
            v.x = jn["localPosition"]["x"].AsFloat;
            v.y = jn["localPosition"]["y"].AsFloat;
            v.z = jn["localPosition"]["z"].AsFloat;
            return v;
        }

        private Quaternion GetLocalRotation(JSONNode jn)
        {
            Vector3 v = new Vector3();
            v.x = jn["localRotation"]["x"].AsFloat;
            v.y = jn["localRotation"]["y"].AsFloat;
            v.z = jn["localRotation"]["z"].AsFloat;
            Quaternion q = new Quaternion();
            q.eulerAngles = v;
            return q;
        }
    }

    public class PoseController : BaseModule
    {
        public JSONStorableBool poseEnabled;
        public JSONStorableBool holdPose;
        public JSONStorableFloat poseAnimationDuration;
        public JSONStorableFloat durationBetweenPoseChange;

        Montage currentMontage;

        Pose startingPose;
        Pose targetPose;
        float animationStartTime = 0;

        float lastTriggeredTime = 0;

        const float CLIMAX_ANIMATION_DURATION = 0.8f;

        public UIDynamicButton nextPoseButton;
        List<FreeControllerV3> controllers;

        public PoseController(DollmasterPlugin dm) : base(dm)
        {
            poseEnabled = new JSONStorableBool("poseEnabled", true);
            dm.RegisterBool(poseEnabled);
            UIDynamicToggle moduleEnableToggle = dm.CreateToggle(poseEnabled);
            moduleEnableToggle.label = "Enable Pose Change";
            moduleEnableToggle.backgroundColor = Color.green;

            poseAnimationDuration = new JSONStorableFloat("poseAnimationDuration", 1.2f, 0.01f, 10.0f);
            dm.RegisterFloat(poseAnimationDuration);
            dm.CreateSlider(poseAnimationDuration);

            durationBetweenPoseChange = new JSONStorableFloat("durationBetweenPoseChange", 8.0f, 1.0f, 20.0f, false);
            dm.RegisterFloat(durationBetweenPoseChange);
            dm.CreateSlider(durationBetweenPoseChange);

            holdPose = new JSONStorableBool("holdPose", false);
            dm.RegisterBool(holdPose);
            UIDynamicToggle holdPoseToggle = ui.CreateToggle("Hold Pose", 180, 40);
            holdPose.toggle = holdPoseToggle.toggle;

            holdPoseToggle.transform.Translate(0.415f, 0.0630f, 0, Space.Self);
            holdPoseToggle.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            holdPoseToggle.labelText.color = new Color(1, 1, 1);

            dm.CreateSpacer();

            controllers = atom.freeControllers.ToList();
        }

        public override void Update()
        {
            base.Update();

            if (poseEnabled.val == false)
            {
                return;
            }

            bool anyHeldByPlayer = controllers.Any((controller) =>
            {
               return controller.linkToRB != null && (controller.linkToRB.name == "MouseGrab" || controller.linkToRB.name == "LeftHand" || controller.linkToRB.name == "RightHand");
            });

            if (anyHeldByPlayer)
            {
                //  delay next pose animation
                lastTriggeredTime = Time.time;
                StopCurrentAnimation();
                return;
            }

            if(startingPose != null && targetPose != null)
            {
                float elapsed = Time.time - animationStartTime;
                float animationDuration = poseAnimationDuration.val;
                if (dm.climaxController.isClimaxing)
                {
                    animationDuration = CLIMAX_ANIMATION_DURATION;
                }
                float alpha = elapsed / animationDuration;
                TweenTransform(startingPose, targetPose, alpha, true);
                if (alpha >= 1)
                {
                    StopCurrentAnimation();
                }
            }
        }

        public void AnimateToPose(Pose pose)
        {
            startingPose = new Pose(atom);
            targetPose = pose;
            animationStartTime = Time.time;
        }

        public void StopCurrentAnimation()
        {
            startingPose = null;
            targetPose = null;
        }

        public void Trigger()
        {
            if (holdPose.val)
            {
                lastTriggeredTime = Time.time;
                return;
            }

            if ((Time.time - lastTriggeredTime) < durationBetweenPoseChange.val)
            {
                return;
            }

            lastTriggeredTime = Time.time;

            if (UnityEngine.Random.Range(0, 100) > 80)
            {
                return;
            }

            dm.montageController.RandomPose();
        }

        static public float CubicEaseInOut(float p)
        {
            if (p < 0.5f)
            {
                return 4 * p * p * p;
            }
            else
            {
                float f = ((2 * p) - 2);
                return 0.5f * f * f * f + 1;
            }
        }

        public void TweenTransform(Pose from, Pose to, float alpha, bool easing = false)
        {
            alpha = Mathf.Clamp01(alpha);

            if (easing)
            {
                alpha = CubicEaseInOut(alpha);
            }

            Transform personRoot = atom.mainController.transform;

            from.transforms.Keys.ToList().ForEach(id =>
            {
                if (from.transforms.ContainsKey(id) == false)
                {
                    return;
                }
                if (to.transforms.ContainsKey(id) == false)
                {
                    return;
                }

                PoseTransform fromT = from.transforms[id];
                PoseTransform toT = to.transforms[id];
                if (fromT == null || toT == null)
                {
                    return;
                }

                JSONStorable storable = atom.GetStorableByID(id);
                storable.transform.localPosition = Vector3.Lerp(fromT.localPosition, toT.localPosition, alpha);
                storable.transform.localRotation = Quaternion.Lerp(fromT.localRotation, toT.localRotation, alpha);
            });
        }

    }
}
