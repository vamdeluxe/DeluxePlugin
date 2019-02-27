using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class PoseController : BaseModule
    {
        public JSONStorableBool poseEnabled;
        public JSONStorableBool holdPose;
        public JSONStorableStringChooser poseChoice;
        public JSONStorableFloat poseAnimationDuration;
        public JSONStorableFloat durationBetweenPoseChange;

        Montage currentMontage;

        JSONClass startingPose;
        JSONClass targetPose;
        float animationStartTime = 0;

        float lastTriggeredTime = 0;

        const float CLIMAX_ANIMATION_DURATION = 0.8f;

        public UIDynamicButton nextPoseButton;

        public PoseController(DollmasterPlugin dm) : base(dm)
        {
            poseEnabled = new JSONStorableBool("poseEnabled", true);
            dm.RegisterBool(poseEnabled);
            UIDynamicToggle moduleEnableToggle = dm.CreateToggle(poseEnabled);
            moduleEnableToggle.label = "Enable Pose Change";
            moduleEnableToggle.backgroundColor = Color.green;

            poseChoice = new JSONStorableStringChooser("poseChoice", new List<string>(), "Default", "Pose", (string poseName)=>
            {
                try
                {
                    if(currentMontage == null)
                    {
                        return;
                    }

                    JSONClass pose = GetPoseFromName(poseName);
                    if (pose == null)
                    {
                        SuperController.LogError("no pose with id " + poseName);
                        return;
                    }

                    AnimateToPose(pose);

                }
                catch(Exception e)
                {
                    Debug.Log(e);
                }

            });

            dm.RegisterStringChooser(poseChoice);



            poseAnimationDuration = new JSONStorableFloat("poseAnimationDuration", 1.2f, 0.01f, 10.0f);
            dm.RegisterFloat(poseAnimationDuration);
            dm.CreateSlider(poseAnimationDuration);

            nextPoseButton = dm.ui.CreateButton("Next Pose", 300, 80);
            nextPoseButton.transform.Translate(0.415f, -0.1f, 0, Space.Self);
            nextPoseButton.buttonColor = new Color(0.4f, 0.3f, 0.05f);
            nextPoseButton.textColor = new Color(1, 1, 1);
            nextPoseButton.button.onClick.AddListener(() =>
            {
                if (currentMontage == null || poseChoice.choices.Count <=1 )
                {
                    return;
                }

                int index = poseChoice.choices.IndexOf(poseChoice.val);
                int nextIndex = index + 1;
                if (nextIndex >= poseChoice.choices.Count)
                {
                    nextIndex = 0;
                }

                poseChoice.SetVal(poseChoice.choices[nextIndex]);
            });
            nextPoseButton.gameObject.SetActive(false);

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
        }

        public void SetMontage(Montage montage)
        {
            currentMontage = montage;
            if (montage == null)
            {
                poseChoice.SetVal(poseChoice.defaultVal);
                return;
            }
            poseChoice.choices = montage.GetPoseNames();
            poseChoice.SetVal(poseChoice.defaultVal);
        }

        public override void Update()
        {
            base.Update();

            if (poseEnabled.val == false)
            {
                return;
            }

            bool anyHeldByPlayer = atom.freeControllers.ToList().Any((controller) =>
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
                    startingPose = null;
                    targetPose = null;
                }
            }
        }

        public JSONClass GetPoseFromName(string poseName)
        {
            JSONClass pose = currentMontage.poses[GetPoseIDFromName(poseName)];
            return pose;
        }

        int GetPoseIDFromName(string poseName)
        {
            return int.Parse(poseName.Split(' ')[1]) - 1;
        }

        public void AnimateToPose(JSONClass pose)
        {
            startingPose = GetLocalPose(atom);
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

            SelectRandomPose();
        }

        public void SelectRandomPose()
        {
            if (currentMontage == null)
            {
                return;
            }

            if (currentMontage.poses.Count == 0)
            {
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, currentMontage.poses.Count);
            JSONClass pose = currentMontage.poses[randomIndex];
            AnimateToPose(pose);
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

        public void TweenTransform(JSONNode from, JSONNode to, float alpha, bool easing = false)
        {
            alpha = Mathf.Clamp01(alpha);

            if (easing)
            {
                alpha = CubicEaseInOut(alpha);
            }

            Transform personRoot = atom.mainController.transform;

            JSONArray fromControllers = from["nodes"].AsArray;
            JSONArray toControllers = to["nodes"].AsArray;

            for (int i = 0; i < fromControllers.Count; i++)
            {
                JSONClass fromStorable = fromControllers[i].AsObject;
                JSONClass toStorable = toControllers[i].AsObject;

                string id = fromStorable.AsObject["id"];

                if (id == "control")
                {
                    continue;
                }

                bool isBone = fromStorable["bone"].AsBool;

                JSONStorable storable = atom.GetStorableByID(id);

                if (storable == null)
                {
                    continue;
                }

                Vector3 fromPosition = GetPosition(fromStorable);
                Quaternion fromRotation = GetRotation(fromStorable);

                Vector3 toPosition = GetPosition(toStorable);
                Quaternion toRotation = GetRotation(toStorable);

                if (isBone)
                {
                    DAZBone bone = storable as DAZBone;
                    if (bone.isRoot)
                    {
                        continue;
                    }
                    continue;
                }

                storable.transform.localPosition = Vector3.Lerp(fromPosition, toPosition, alpha);
                storable.transform.localRotation = Quaternion.Lerp(fromRotation, toRotation, alpha);
            }
        }

        private Vector3 GetPosition(JSONNode jn)
        {
            Vector3 v = new Vector3();
            v.x = jn["position"]["x"].AsFloat;
            v.y = jn["position"]["y"].AsFloat;
            v.z = jn["position"]["z"].AsFloat;
            return v;
        }

        private Quaternion GetRotation(JSONNode jn)
        {
            Vector3 v = new Vector3();
            v.x = jn["rotation"]["x"].AsFloat;
            v.y = jn["rotation"]["y"].AsFloat;
            v.z = jn["rotation"]["z"].AsFloat;
            Quaternion q = new Quaternion();
            q.eulerAngles = v;
            return q;
        }

        public static JSONClass GetLocalPose(Atom person)
        {
            JSONClass obj = new JSONClass();
            JSONArray saveArray = new JSONArray();
            obj["nodes"] = saveArray;

            person.GetStorableIDs().ToList().ForEach((storeId) =>
            {
                DAZBone bone = person.GetStorableByID(storeId) as DAZBone;
                if (bone == null)
                {
                    return;
                }

                JSONClass controllerNode = new JSONClass();
                controllerNode["id"] = bone.storeId;
                controllerNode["bone"].AsBool = true;

                Vector3 position = bone.transform.localPosition;
                controllerNode["position"]["x"].AsFloat = position.x;
                controllerNode["position"]["y"].AsFloat = position.y;
                controllerNode["position"]["z"].AsFloat = position.z;
                Vector3 eulerAngles = bone.transform.localEulerAngles;
                controllerNode["rotation"]["x"].AsFloat = eulerAngles.x;
                controllerNode["rotation"]["y"].AsFloat = eulerAngles.y;
                controllerNode["rotation"]["z"].AsFloat = eulerAngles.z;


                saveArray.Add(controllerNode);
            });

            person.freeControllers.ToList().ForEach((controller) =>
            {
                JSONClass controllerNode = new JSONClass();
                controllerNode["id"] = controller.storeId;
                controllerNode["bone"].AsBool = false;

                Vector3 position = controller.transform.localPosition;
                controllerNode["position"]["x"].AsFloat = position.x;
                controllerNode["position"]["y"].AsFloat = position.y;
                controllerNode["position"]["z"].AsFloat = position.z;

                Vector3 eulerAngles = controller.transform.localEulerAngles;
                controllerNode["rotation"]["x"].AsFloat = eulerAngles.x;
                controllerNode["rotation"]["y"].AsFloat = eulerAngles.y;
                controllerNode["rotation"]["z"].AsFloat = eulerAngles.z;

                saveArray.Add(controllerNode);
            });
            return obj;
        }

    }
}
