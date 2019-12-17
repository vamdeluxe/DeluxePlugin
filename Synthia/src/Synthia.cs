using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SimpleJSON;

namespace DeluxePlugin.Synthia
{
    public class Synthia : MVRScript
    {
        const string INSTRUCTIONS =
@"Synthia is a character animation system.

Your selected person will now respond to commands. Right click (desktop) or Trigger (VR, only when menu is open) commands the person to walk to some point in space.
Upon arrival, the idle animation will play.

+ Turn rate adjusts how quickly the person faces the new target.

+ Heel height and angle adjusts the foot during animation, useful if your person is wearing high heels.

+ Ignore feet collisions will cause left and right feet (and shins) to not hit each other during animation.

+ Show target will show a small ball where you want your person to move to.

+ 'Walk uses root motion up down' causes the person to move up and down if the animation move up and down. Useful for dances. Be careful of collisions with the ground as it my crash VAM.

You can also replace animations with your own BVH animations. This requires the idle and walk to be aligned towards the Z axis.

This plugin is built on the work by ElkVR.
";

        public AnimationBank animations;

        public BVHPlayer animator;
        Transform goalMarker;

        JSONStorableStringChooser idleAnimation;
        JSONStorableStringChooser walkAnimation;
        JSONStorableStringChooser forceAnimation;

        JSONStorableFloat playbackSpeed;
        JSONStorableBool moveToTarget;
        JSONStorableFloat turnRate;
        JSONStorableFloat heelHeight;
        JSONStorableFloat heelAngle;
        JSONStorableBool ignoreFeetCollisions;
        JSONStorableBool useRecommendedPhyics;
        JSONStorableBool showTarget;
        JSONStorableBool selectRandomAnimations;

        Animation forcePlayAnimation;

        UIDynamicPopup idlePopup;
        UIDynamicPopup walkPopup;

        string lastDir = "Custom";

        public override void Init()
        {
            try
            {
                var addAnimationButton = CreateButton("Add Animation");
                addAnimationButton.buttonColor = new Color(0.75f, 0.44f, 0.1f);
                addAnimationButton.button.onClick.AddListener(() =>
                {
                    string folder = lastDir;
                    SuperController.singleton.GetMediaPathDialog((string path) =>
                    {
                        if (String.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        Animation animation = new Animation(path, this)
                        {
                            loop = true,
                            restartOnAnimationChange = true,
                            rootMotionXZ = true,
                            rootMotionY = true,
                        };
                        animations.Add(animation);


                        idleAnimation.choices = animations.GetIdList();
                        walkAnimation.choices = animations.GetIdList();
                        forceAnimation.choices = animations.GetIdList();

                        int startIndex = 0;
                        int endIndex = path.LastIndexOf("\\");
                        int length = endIndex - startIndex + 1;
                        string pathWithoutFile = path.Substring(startIndex, length);
                        lastDir = pathWithoutFile;
                    }, "bvh", folder, false);
                });

                #region Default Animations
                animations = new AnimationBank(this);
                string ANIMATION_PATH = GetPluginPath() + "/Animations/";

                animations.Add(new Animation(ANIMATION_PATH + "Look.bvh", this, "Idle")
                {
                    loop = true,
                    startFrame = 2,
                });

                animations.Add(new Animation(ANIMATION_PATH + "StandToWalk.bvh", this, "Walk")
                {
                    loop = true,
                    restartOnAnimationChange = true,
                    rootMotionXZ = true,
                    rootMotionY = false,
                    startFrame = 67,
                    endFrame = 150,
                });
                #endregion


                playbackSpeed = new JSONStorableFloat("playback speed", 1, (float speed)=>
                {
                    animator.playbackMultiplier = speed;
                }, 0, 4, false);
                RegisterFloat(playbackSpeed);
                CreateSlider(playbackSpeed, true);

                #region Walk Cycle
                moveToTarget = new JSONStorableBool("walk to target", true);
                RegisterBool(moveToTarget);
                CreateToggle(moveToTarget, true);

                idleAnimation = new JSONStorableStringChooser("idleAnimation", animations.GetIdList(), "Idle", "Idle Animation");
                RegisterStringChooser(idleAnimation);
                idlePopup = CreatePopup(idleAnimation, true);
                idlePopup.popup.onOpenPopupHandlers += () => {
                    idleAnimation.choices = animations.GetIdList();
                };

                walkAnimation = new JSONStorableStringChooser("walkAnimation", animations.GetIdList(), "Walk", "Walk Animation");
                RegisterStringChooser(walkAnimation);
                walkPopup = CreatePopup(walkAnimation, true);
                walkPopup.popup.onOpenPopupHandlers += () => {
                    walkAnimation.choices = animations.GetIdList();
                };

                forceAnimation = new JSONStorableStringChooser("forcedAnimationChoice", animations.GetIdList(), "Idle", "Trigger Force Animation", (string choice)=>
                {
                    ForcePlayAnimation(animations.Get(choice));
                });
                RegisterStringChooser(forceAnimation);

                turnRate = new JSONStorableFloat("turn rate", 6, 0.01f, 12f, false);
                RegisterFloat(turnRate);
                CreateSlider(turnRate, true);

                heelHeight = new JSONStorableFloat("heel height", 0, SetHeelHeight, 0, 0.2f, true);
                RegisterFloat(heelHeight);
                CreateSlider(heelHeight, true);

                heelAngle = new JSONStorableFloat("heel angle", 30, SetHeelAngle, 0, 120, true);
                RegisterFloat(heelAngle);
                CreateSlider(heelAngle, true);

                ignoreFeetCollisions = new JSONStorableBool("ignore feet collisions", true, SetIgnoreFeetCollisions);
                RegisterBool(ignoreFeetCollisions);
                CreateToggle(ignoreFeetCollisions, true);

                useRecommendedPhyics = new JSONStorableBool("use recommended physics", false, SetRecommendedPhysics);
                RegisterBool(useRecommendedPhyics);
                CreateToggle(useRecommendedPhyics, true);

                showTarget = new JSONStorableBool("show target", true, SetTargetVisibility);
                RegisterBool(showTarget);
                CreateToggle(showTarget, true);
                goalMarker = CreateMarker(containingAtom.transform);

                selectRandomAnimations = new JSONStorableBool("select random animations", false);
                RegisterBool(selectRandomAnimations);
                CreateToggle(selectRandomAnimations, true);

                #endregion



                //var instructions = CreateTextField(new JSONStorableString("instructions", INSTRUCTIONS), true);
                //instructions.height = 1100;
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Start()
        {
            // For some reason we need to do this before ResetPhysical (in BVHPlayer constructor) and then again after to take effect.
            SetRecommendedPhysics(useRecommendedPhyics.val);

            animator = new BVHPlayer(containingAtom);
            animator.Play(animations.Get(idleAnimation.val));

            SetRecommendedPhysics(useRecommendedPhyics.val);

            // Re-do all the settings because animator calls ResetPhysical which resets all the plugin values.
            // This is a VAM bug.
            SetIgnoreFeetCollisions(ignoreFeetCollisions.val);
            SetHeelHeight(heelHeight.val);
            SetHeelAngle(heelAngle.val);
            SetTargetVisibility(showTarget.val);
        }

        void Update()
        {
            foreach(var entry in animations.bank)
            {
                var animation = entry.Value;
                animation.UpdateDebug();
            }

            if (moveToTarget.val)
            {
                MovementInput();
                goalMarker.transform.position = goal;
            }
            else
            {
                SetTargetVisibility(false);
            }

            if(animator.finished && selectRandomAnimations.val)
            {
                var animationIds = animations.GetIdList();
                if (animationIds.Count > 0)
                {
                    int index = UnityEngine.Random.Range(0, animationIds.Count);
                    Animation animation = animations.Get(animationIds[index]);
                    ForcePlayAnimation(animation);
                }
            }
        }

        void FixedUpdate()
        {
            if (animator != null)
            {
                animator.FixedUpdate();
                animator.ApplyRootMotion(0);
            }

            if (moveToTarget.val)
            {
                if (forcePlayAnimation == null)
                {
                    MoveTowardsGoal();
                    RotateToGoal();
                }
            }
        }

        public Vector3 goal = new Vector3(0, 0, 0);
        public Collider groundCollider;
        public float speed = 0;
        public float velocity = 0;
        public float torque = 0;
        Vector3 lastPosition;
        Vector3 lastEuler;
        public float distanceToGoal
        {
            get
            {
                return (goal - containingAtom.transform.position).magnitude;
            }
        }

        private const float GOAL_REACHED_EPISILON = 0.25f;

        void MovementInput()
        {
            if (SuperController.singleton.GetSelectedAtom() != containingAtom)
            {
                return;
            }

            if (SuperController.singleton.GetLeftGrabVal()>0 || SuperController.singleton.GetRightGrabVal()>0)
            {
                SuperController.singleton.rayLineLeft.gameObject.SetActive(true);
                SuperController.singleton.rayLineRight.gameObject.SetActive(true);

                Transform lineLeft = SuperController.singleton.rayLineLeft.transform;
                Transform lineRight = SuperController.singleton.rayLineRight.transform;
                Ray rayLeft = new Ray(lineLeft.transform.position, lineLeft.transform.rotation * Vector3.forward);
                Ray rayRight = new Ray(lineRight.transform.position, lineRight.transform.rotation * Vector3.forward);
                ProcessRay(rayLeft);
                ProcessRay(rayRight);
                ClearForcePlayAnimation();
            }

            if (Input.GetMouseButtonDown(1))
            {
                ProcessRay(Camera.main.ScreenPointToRay(Input.mousePosition));
                ClearForcePlayAnimation();
            }
        }

        void ProcessRay(Ray ray)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.GetType() != typeof(BoxCollider))
                {
                    return;
                }
                groundCollider = hit.collider;
                goal = hit.point;
            }
        }

        Transform CreateMarker(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            go.parent = parent;
            go.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            go.localPosition = Vector3.zero;
            go.localRotation = Quaternion.identity;
            GameObject.Destroy(go.GetComponent<SphereCollider>());
            return go;
        }

        void MoveTowardsGoal()
        {
            Vector3 delta = goal - containingAtom.mainController.transform.position;
            delta.y = 0;

            if (delta.magnitude > GOAL_REACHED_EPISILON)
            {
                animator.Play(animations.Get(walkAnimation.val));
            }
            else
            {
                animator.Play(animations.Get(idleAnimation.val));
            }
        }

        private void RotateToGoal()
        {
            Vector3 delta = goal - containingAtom.mainController.transform.position;
            if (delta.magnitude < 0.1f)
            {
                return;
            }

            Quaternion lookAt = Quaternion.LookRotation(delta);
            Vector3 lookAtEuler = lookAt.eulerAngles;
            lookAtEuler.x = 0;
            lookAt.eulerAngles = lookAtEuler;

            Quaternion current = containingAtom.mainController.transform.rotation;
            containingAtom.mainController.transform.rotation = Quaternion.RotateTowards(current, lookAt, turnRate.val);
        }

        void OnDestroy()
        {
            if (animator != null)
            {
                animator.OnDestroy();
            }

            if (goalMarker != null)
            {
                GameObject.Destroy(goalMarker.gameObject);
            }
        }

        string GetPluginPath()
        {
            SuperController.singleton.currentSaveDir = SuperController.singleton.currentLoadDir;
            string pluginId = this.storeId.Split('_')[0];
            MVRPluginManager manager = containingAtom.GetStorableByID("PluginManager") as MVRPluginManager;
            string pathToScriptFile = manager.GetJSON(true, true)["plugins"][pluginId].Value;
            string pathToScriptFolder = pathToScriptFile.Substring(0, pathToScriptFile.LastIndexOfAny(new char[] { '/', '\\' }));
            return pathToScriptFolder;
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass jSON = base.GetJSON(includePhysical, includeAppearance);
            JSONClass animationsJSON = new JSONClass();
            animations.bank.Keys.ToList().ForEach(id =>
            {
                Animation animation = animations.Get(id);
                animationsJSON[id] = animation.Serialize();
            });
            jSON["animations"] = animationsJSON;
            needsStore = true;
            return jSON;
        }

        public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms);
            needsStore = true;
            JSONClass animationsJSON = jc["animations"] as JSONClass;
            if (animationsJSON == null)
            {
                return;
            }

            animationsJSON.Keys.ToList().ForEach(id =>
            {
                JSONClass data = animationsJSON[id].AsObject;
                Animation animation = animations.Get(id);
                if (animation == null)
                {
                    animation = new Animation(data["path"], this, id);
                    animation.Deserialize(data);
                    animations.Add(animation);
                }
                else
                {
                    animation.Deserialize(data);
                }
            });

            idleAnimation.choices = animations.GetIdList();
            walkAnimation.choices = animations.GetIdList();
            forceAnimation.choices = animations.GetIdList();
        }

        void SetIgnoreFeetCollisions(bool ignoring)
        {
            DAZBone rFoot = containingAtom.GetStorableByID("rFoot") as DAZBone;
            List<Collider> rColliders = rFoot.GetComponentsInChildren<Collider>().ToList();
            DAZBone lFoot = containingAtom.GetStorableByID("lFoot") as DAZBone;
            List<Collider> lColliders = lFoot.GetComponentsInChildren<Collider>().ToList();

            rColliders.ForEach(rCollider =>
            {
                lColliders.ForEach(lCollider =>
                {
                    Physics.IgnoreCollision(rCollider, lCollider, ignoring);
                });
            });
        }

        void SetHeelHeight(float meters)
        {
            if (animator != null)
            {
                animator.heelHeight = heelHeight.val;
            }
        }

        void SetHeelAngle(float degrees)
        {
            if (animator != null)
            {
                animator.heelAngle = heelAngle.val;
            }

            containingAtom.freeControllers.Where(controller =>
            {
                return controller.name.Contains("Toe");
            }).ToList()
            .ForEach(controller =>
            {
                controller.jointRotationDriveXTarget = degrees * 0.5f;
            });
        }

        void SetRecommendedPhysics(bool use)
        {
            if (use == false)
            {
                return;
            }

            containingAtom.freeControllers.ToList().ForEach(controller =>
            {
                controller.SetHoldPositionSpringPercent(0.15f);
                controller.SetHoldRotationSpringPercent(0.15f);
                controller.SetHoldPositionDamperPercent(0.6f);
                controller.SetHoldRotationDamperPercent(0.6f);
            });

            containingAtom.freeControllers
            .Where(controller =>
            {
                return controller.name.Contains("hip");
            }).ToList()
            .ForEach(controller =>
            {
                controller.SetHoldPositionSpringPercent(0.9f);
                controller.SetHoldRotationSpringPercent(0.9f);
                controller.SetHoldPositionDamperPercent(0.4f);
                controller.SetHoldRotationDamperPercent(0.4f);
            });

            containingAtom.freeControllers
            .Where(controller =>
            {
                return controller.name.Contains("Foot");
            }).ToList()
            .ForEach(controller =>
            {
                controller.SetHoldPositionSpringPercent(0.1f);
                controller.SetHoldRotationSpringPercent(0.9f);
                controller.SetHoldPositionDamperPercent(0.8f);
                controller.SetHoldRotationDamperPercent(0.2f);
            });

            containingAtom.freeControllers.Where(controller =>
            {
                return controller.name.Contains("Toe");
            }).ToList()
            .ForEach(controller =>
            {
                controller.jointRotationDriveSpring = 40;
                controller.jointRotationDriveMaxForce = 100;
            });

        }

        void SetTargetVisibility(bool visible)
        {
            if (goalMarker == null)
            {
                return;
            }

            goalMarker.gameObject.SetActive(visible);
        }

        public void ForcePlayAnimation(Animation animation)
        {
            forcePlayAnimation = animation;
            animator.Play(forcePlayAnimation);
        }

        public void ClearForcePlayAnimation()
        {
            forcePlayAnimation = null;
        }
    }
}