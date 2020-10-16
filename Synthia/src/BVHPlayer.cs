using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using SimpleJSON;

namespace DeluxePlugin.Synthia
{
    // Original script by ElkVR
    // Adapted in Synthia by VAMDeluxe
    public class BVHPlayer
    {
        Atom containingAtom;

        Dictionary<string, FreeControllerV3> controllerMap;

        Dictionary<string, string> cnameToBname = new Dictionary<string, string>() {
        { "hipControl", "hip" },
        //{ "headControl", "head" },
        { "chestControl", "chest" },
        { "lHandControl", "lHand" },
        { "rHandControl", "rHand" },
        { "lFootControl", "lFoot" },
        { "rFootControl", "rFoot" },
        { "lKneeControl", "lShin" },
        { "rKneeControl", "rShin" },
        //{ "neckControl", "neck" },
        { "lElbowControl", "lForeArm" },
        { "rElbowControl", "rForeArm" },
        { "lArmControl", "lShldr" },
        { "rArmControl", "rShldr" },
        // Additional bones
        { "lShoulderControl", "lCollar" },
        { "rShoulderControl", "rCollar" },
        { "abdomenControl", "abdomen" },
        { "abdomen2Control", "abdomen2" },
        { "pelvisControl", "pelvis" },
        { "lThighControl", "lThigh" },
        { "rThighControl", "rThigh" },
        // { "lToeControl", "lToe" },
        // { "rToeControl", "rToe" },
    };

        Transform shadow = null;

        public Animation animation;
        BvhFile bvh = null;
        float elapsed = 0;

        public int frame = 0;
        public bool playing = false;

        bool loopPlay = true;
        bool onlyHipTranslation = true;
        float frameTime;

        // Apparently we shouldn't use enums because it causes a compiler crash
        const int translationModeOffsetPlusFrame = 0;
        const int translationModeFrameOnly = 1;
        const int translationModeInitialPlusFrameMinusOffset = 2;
        const int translationModeInitialPlusFrameMinusZero = 3;

        int translationMode = translationModeInitialPlusFrameMinusZero;

        public Vector3 rootMotion;

        public float heelHeight = 0;
        public float heelAngle = 0;

        public BVHPlayer(Atom atom)
        {
            containingAtom = atom;
            containingAtom.ResetPhysical();
            CreateShadowSkeleton();
            RecordOffsets();
            CreateControllerMap();
        }

        public void Play(Animation animation)
        {
            if (this.animation == animation)
            {
                return;
            }

            if (animation.restartOnAnimationChange)
            {
                frame = 0;
                //SuperController.LogMessage("restarting");
            }

            this.animation = animation;
            bvh = animation.bvh;
            frameTime = bvh.frameTime;

            CreateControllerMap();

            rootMotion = new Vector3();
            playing = true;
            loopPlay = animation.loop;
        }

        void CreateControllerMap()
        {
            controllerMap = new Dictionary<string, FreeControllerV3>();
            foreach (FreeControllerV3 controller in containingAtom.freeControllers)
                controllerMap[controller.name] = controller;

            foreach (var item in cnameToBname)
            {
                var c = controllerMap[item.Key];
                c.currentRotationState = FreeControllerV3.RotationState.On;
                c.currentPositionState = FreeControllerV3.PositionState.On;
            }
        }

        Transform CreateMarker(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            go.parent = parent;
            go.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            go.localPosition = Vector3.zero;
            go.localRotation = Quaternion.identity;
            GameObject.Destroy(go.GetComponent<BoxCollider>());
            return go;
        }

        List<Transform> markers = null;

        public void ShowSkeleton()
        {
            if (markers != null)
                HideSkeleton();
            markers = new List<Transform>();
            foreach (var bone in bones)
                markers.Add(CreateMarker(bone.Value));
        }

        public void HideSkeleton()
        {
            foreach (var marker in markers)
                GameObject.Destroy(marker.gameObject);
            markers = null;
        }

        Dictionary<string, Transform> bones;
        Dictionary<string, Vector3> tposeBoneOffsets = null;

        void RecordOffsets()
        {
            CreateShadowSkeleton();
            tposeBoneOffsets = new Dictionary<string, Vector3>();
            foreach (var item in bones)
                tposeBoneOffsets[item.Key] = item.Value.localPosition;
        }

        public void CreateShadow(Transform skeleton, Transform shadow)
        {
            bones[shadow.gameObject.name] = shadow;
            shadow.localPosition = skeleton.localPosition;
            shadow.localRotation = skeleton.localRotation;
            for (var i = 0; i < skeleton.childCount; i++)
            {
                var child = skeleton.GetChild(i);
                if (child.gameObject.GetComponent<DAZBone>() != null)
                {
                    var n = new GameObject(child.gameObject.name).transform;
                    n.parent = shadow;
                    CreateShadow(child, n);
                }
            }
        }

        void CreateShadowSkeleton()
        {
            foreach (var parent in containingAtom.gameObject.GetComponentsInChildren<DAZBones>())
            {
                // SuperController.LogMessage(string.Format("{0}", parent.gameObject.name));
                // SuperController.LogMessage(parent.gameObject.name);
                if (shadow != null)
                    GameObject.Destroy(shadow.gameObject);
                bones = new Dictionary<string, Transform>();
                shadow = new GameObject("Shadow").transform;
                shadow.position = parent.transform.position;
                CreateShadow(parent.gameObject.transform, shadow);
            }
        }

        BvhTransform[] Interpolate(BvhTransform[] a, BvhTransform[] b, float t)
        {
            var ret = new BvhTransform[a.Length];
            for (var i = 0; i < a.Length; i++)
            {
                var at = a[i];
                var bt = b[i];

                var res = new BvhTransform();
                res.bone = at.bone;
                res.position = Vector3.Lerp(at.position, bt.position, t);
                res.rotation = Quaternion.Lerp(at.rotation, bt.rotation, t);

                ret[i] = res;

                if (res.bone.isHipBone)
                {
                    rootMotion = (bt.position - at.position) * t;
                }
            }
            return ret;
        }

        void UpdateModel(BvhTransform[] data)
        {
            foreach (var item in data)
            {
                // Copy on to model
                if (bones.ContainsKey(item.bone.name))
                {
                    bones[item.bone.name].localRotation = item.rotation;
                }
            }
        }

        public void ApplyRootMotion(float applyYaw = 0)
        {
            int xz = animation.rootMotionXZ ? 1 : 0;
            int y = animation.rootMotionY ? 1 : 0;

            Vector3 rootMotion2D = new Vector3(rootMotion.x * xz, rootMotion.y * y, rootMotion.z * xz);
            rootMotion2D = Quaternion.AngleAxis(applyYaw, Vector3.up) * rootMotion2D;
            containingAtom.mainController.transform.Translate(rootMotion2D);
        }

        public void FixedUpdate()
        {
            try
            {
                if (bvh == null || bvh.nFrames == 0)
                    return;

                rootMotion = new Vector3();

                FrameAdvance();

                foreach (var item in cnameToBname)
                {
                    controllerMap[item.Key].transform.localPosition = bones[item.Value].position;
                    controllerMap[item.Key].transform.localRotation = bones[item.Value].rotation;
                    controllerMap[item.Key].transform.localPosition += new Vector3(0, heelHeight, 0);
                    if (item.Key.Contains("Foot"))
                    {
                        controllerMap[item.Key].transform.localEulerAngles += new Vector3(heelAngle, 0, 0);
                    }

                    if (item.Key.Contains("Toe"))
                    {
                        //controllerMap[item.Key].jointRotationDriveXTargetAdditional = heelAngle * 0.5f;
                        controllerMap[item.Key].transform.localEulerAngles += new Vector3(-heelAngle, 0, 0);
                    }
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Fixed Update: " + e);
            }
        }

        public void FrameAdvance()
        {
            if (playing)
            {
                elapsed += Time.fixedDeltaTime;
                if (elapsed >= frameTime)
                {
                    elapsed = 0;
                    frame++;
                }
            }

            if (frame < animation.startFrame)
            {
                frame = animation.startFrame;
            }

            if (frame >= animation.endFrame)
            {
                if (loopPlay == false)
                {
                    playing = false;
                    return;
                }
                else
                {
                    frame = animation.startFrame;
                }
            }

            //SuperController.LogMessage(frame + " / " + bvh.nFrames);

            if (frame >= animation.endFrame - 1)
            {
                // Last frame
                UpdateModel(bvh.ReadFrame(frame));
            }
            else
            {
                // Interpolate
                var frm = bvh.ReadFrame(frame);
                var to = bvh.ReadFrame(frame + 1);

                float t = elapsed / frameTime;
                UpdateModel(Interpolate(frm, to, t));
            }
        }

        public void OnDestroy()
        {
            if (shadow != null)
            {
                GameObject.Destroy(shadow.gameObject);
            }
        }
    }
}
