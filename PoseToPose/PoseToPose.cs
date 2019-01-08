using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.PoseToPose
{
    public class PoseToPose : MVRScript
    {
        string PATH_WHEN_LOADED = "";
        Atom person = null;
        List<Keyframe> keyframes = new List<Keyframe>();

        JSONStorableBool uiVisible;
        JSONStorableString saveStore;
        JSONClass saveJSON;
        JSONStorableBool lookAtCamera;

        AnimationPattern animationPattern;

        JSONStorableFloat time;
        UIDynamicSlider timeSlider;
        UIDynamicSlider setDurationSlider;
        UIDynamicPopup tweenSelection;
        UIDynamicButton undoPoseButton;

        /// <summary>
        /// The keyframe actively selected by user (clicked on).
        /// </summary>
        Keyframe selectedKeyframe;

        /// <summary>
        /// The last keyframe that was selected, could be null as well.
        /// </summary>
        Keyframe lastSelectedKeyframe;

        /// <summary>
        /// Which keyframe was last selected, regardless of the current selection.
        /// Using this to maintain editing of a keyframe while not having the keyframe itself selected.
        /// </summary>
        Keyframe editingKeyframe;

        /// <summary>
        /// For undo.
        /// </summary>
        JSONClass lastPose;

        /// <summary>
        /// For copy paste.
        /// </summary>
        static JSONClass copiedPose;

        static Vector3 copiedPosition;
        static Quaternion copiedRotation;

        UI ui;
        UI keyUI;

        Coroutine updateKeyframeLoop;

        LineRenderer thickTimeline;

        public override void Init()
        {
            try
            {
                if (containingAtom == null)
                {
                    return;
                }


                PATH_WHEN_LOADED = SuperController.singleton.currentLoadDir;
                SuperController.singleton.currentSaveDir = PATH_WHEN_LOADED;

                uiVisible = new JSONStorableBool("uiVisible", false, (bool on)=>
                {
                    ui.canvas.enabled = on;
                    keyUI.canvas.enabled = on;
                });
                RegisterBool(uiVisible);


                #region Animation Pattern Init
                animationPattern = containingAtom.GetStorableByID("AnimationPattern") as AnimationPattern;
                if (animationPattern == null)
                {
                    SuperController.LogError("You must add this plugin to an AnimationPattern");
                    return;
                }
                animationPattern.Pause();
                animationPattern.containingAtom.GetStorableByID("scale").SetFloatParamValue("scale", 0);
                #endregion


                CreatePeopleChooser((atom) =>
                {
                    if (atom == null)
                    {
                        return;
                    }
                    person = atom;
                });

                saveStore = new JSONStorableString("keyframes", "");
                RegisterString(saveStore);

                #region World UI
                float UISCALE = 0.001f;
                ui = new UI(this, UISCALE);
                ui.canvas.transform.SetParent(animationPattern.containingAtom.mainController.transform, false);

                UIDynamicToggle onToggle = ui.CreateToggle("On", 120, 60);
                JSONStorableBool onStorable = animationPattern.GetBoolJSONParam("on");
                onStorable.toggleAlt = onToggle.toggle;
                onToggle.transform.localPosition = new Vector3(-575+35, 160, 0);

                UIDynamicToggle loopToggle = ui.CreateToggle("Loop", 140, 60);
                JSONStorableBool loopStorable = animationPattern.GetBoolJSONParam("loop");
                loopStorable.toggleAlt = loopToggle.toggle;
                loopToggle.transform.localPosition = new Vector3(-575 + 180, 160, 0);

                UIDynamicButton doneEditingButton = ui.CreateButton("Done Editing", 200, 60);
                doneEditingButton.transform.localPosition = new Vector3(-575 + 75, -300, 0);
                doneEditingButton.buttonColor = new Color(0.45f, 0.8f, 0.3f);
                doneEditingButton.textColor = new Color(1, 1, 1);
                doneEditingButton.button.onClick.AddListener(() =>
                {
                    uiVisible.SetVal(false);
                    ClearEditingKey();
                    if (SuperController.singleton.GetSelectedAtom() == animationPattern.containingAtom)
                    {
                        SuperController.singleton.ClearSelection();
                    }
                });

                timeSlider = ui.CreateSlider("Time", 500, 110);
                timeSlider.transform.localPosition = new Vector3(-350, 55, 0);
                timeSlider.slider.maxValue = animationPattern.GetTotalTime();

                time = animationPattern.GetFloatJSONParam("currentTime");
                time.sliderAlt = timeSlider.slider;

                UIDynamicButton playButton = ui.CreateButton("▶", 50, 60);
                playButton.transform.localPosition = new Vector3(-575, -30, 0);
                playButton.button.onClick.AddListener(() =>
                {
                    ClearEditingKey();
                    if(animationPattern.GetCurrentTimeCounter() == animationPattern.GetTotalTime())
                    {
                        animationPattern.ResetAndPlay();
                    }
                    else
                    {
                        animationPattern.Play();
                    }
                });

                UIDynamicButton pauseButton = ui.CreateButton("||", 50, 60);
                pauseButton.transform.localPosition = new Vector3(-525, -30, 0);
                pauseButton.button.onClick.AddListener(() =>
                {
                    ClearEditingKey();
                    animationPattern.Pause();
                });

                UIDynamicButton rewindButton = ui.CreateButton("«", 50, 60);
                rewindButton.transform.localPosition = new Vector3(-475, -30, 0);
                rewindButton.button.onClick.AddListener(() =>
                {
                    ClearEditingKey();
                    animationPattern.ResetAnimation();
                });

                UIDynamicButton addKeyButton = ui.CreateButton("+Key", 100, 60);
                addKeyButton.textColor = new Color(0.984f, 0.917f, 0.972f);
                addKeyButton.buttonColor = new Color(0.474f, 0.023f, 0.4f);
                addKeyButton.transform.localPosition = new Vector3(-550, -200, 0);
                addKeyButton.button.onClick.AddListener(AddKey);

                UIDynamicButton insertKeyButton = ui.CreateButton("Insert Key", 140, 60);
                insertKeyButton.textColor = new Color(0.984f, 0.917f, 0.972f);
                insertKeyButton.buttonColor = new Color(0.474f, 0.023f, 0.4f);
                insertKeyButton.transform.localPosition = new Vector3(-420, -200, 0);
                insertKeyButton.button.onClick.AddListener(InsertKey);

                UIDynamicButton nextKeyButton = ui.CreateButton("Next Key", 140, 60);
                nextKeyButton.transform.localPosition = new Vector3(-390, -100, 0);
                nextKeyButton.button.onClick.AddListener(() =>
                {
                    if (keyframes.Count <= 0)
                    {
                        return;
                    }

                    int index = keyframes.IndexOf(GetNearestKeyToTime(time.val));
                    int nextIndex = index + 1;
                    if (nextIndex >= keyframes.Count)
                    {
                        nextIndex = 0;
                    }

                    SuperController.singleton.SelectController(keyframes[nextIndex].step.containingAtom.mainController);
                });

                UIDynamicButton previousKeyButton = ui.CreateButton("Prev Key", 140, 60);
                previousKeyButton.transform.localPosition = new Vector3(-530, -100, 0);
                previousKeyButton.button.onClick.AddListener(() =>
                {
                    if (keyframes.Count <= 0)
                    {
                        return;
                    }

                    int index = keyframes.IndexOf(GetNearestKeyToTime(time.val));
                    int prevIndex = index - 1;

                    if (prevIndex < 0)
                    {
                        prevIndex = keyframes.Count-1;
                    }

                    SuperController.singleton.SelectController(keyframes[prevIndex].step.containingAtom.mainController);
                });




                #endregion

                #region Keyframe UI
                keyUI = new UI(this, UISCALE * 0.8f);
                undoPoseButton = keyUI.CreateButton("Undo", 300, 90);
                undoPoseButton.buttonText.fontSize = 40;
                undoPoseButton.transform.localPosition = new Vector3(0, 120, 0);
                undoPoseButton.textColor = new Color(1, 1, 1);
                undoPoseButton.buttonColor = new Color(0.650f, 0.027f, 0.027f);
                undoPoseButton.button.onClick.AddListener(() =>
                {
                    if (editingKeyframe == null)
                    {
                        return;
                    }

                    editingKeyframe.pose = lastPose;
                    SetToPose(lastPose, true);
                    UpdateSaveStore();
                });

                setDurationSlider = keyUI.CreateSlider("Duration To Key", 500, 110);
                setDurationSlider.defaultButtonEnabled = true;
                setDurationSlider.quickButtonsEnabled = true;
                setDurationSlider.rangeAdjustEnabled = true;
                setDurationSlider.transform.localPosition = new Vector3(0, -270, 0);

                tweenSelection = keyUI.CreatePopup("Tween", 500, 110);
                tweenSelection.transform.localPosition = new Vector3(0, -350, 0);

                UIDynamicButton copyPoseButton = keyUI.CreateButton("Copy Pose", 200, 60);
                copyPoseButton.transform.localPosition = new Vector3(-150, -170, 0);
                copyPoseButton.button.onClick.AddListener(() =>
                {
                    if (editingKeyframe == null)
                    {
                        return;
                    }

                    copiedPose = editingKeyframe.pose;
                });

                UIDynamicButton pastePoseButton = keyUI.CreateButton("Paste Pose", 200, 60);
                pastePoseButton.transform.localPosition = new Vector3(50, -170, 0);
                pastePoseButton.button.onClick.AddListener(() =>
                {
                    if (editingKeyframe == null)
                    {
                        return;
                    }

                    if (copiedPose == null)
                    {
                        return;
                    }

                    editingKeyframe.pose = copiedPose;
                    SetToPose(editingKeyframe.pose, true);
                    UpdateSaveStore();
                });

                UIDynamicButton copyControllerButton = keyUI.CreateButton("Copy Control", 200, 60);
                copyControllerButton.transform.localPosition = new Vector3(-150, -120, 0);
                copyControllerButton.button.onClick.AddListener(() =>
                {
                    if (editingKeyframe == null)
                    {
                        return;
                    }

                    FreeControllerV3 selectedController = SuperController.singleton.GetSelectedController();
                    if (selectedController  == null)
                    {
                        return;
                    }

                    copiedPosition = selectedController.transform.position;
                    copiedRotation = selectedController.transform.rotation;
                });

                UIDynamicButton pasteControllerButton = keyUI.CreateButton("Paste Control", 200, 60);
                pasteControllerButton.transform.localPosition = new Vector3(50, -120, 0);
                pasteControllerButton.button.onClick.AddListener(() =>
                {
                    if (editingKeyframe == null)
                    {
                        return;
                    }

                    FreeControllerV3 selectedController = SuperController.singleton.GetSelectedController();
                    if (selectedController == null)
                    {
                        return;
                    }

                    selectedController.transform.position = copiedPosition;
                    selectedController.transform.rotation = copiedRotation;
                });

                #endregion

                #region Plugin (Debug) UI
                #region Experimental Save and Load Pose
                UIDynamicButton loadPoseButton = CreateButton("Load Pose");
                loadPoseButton.transform.localPosition = new Vector3(-520, -150, 0);
                loadPoseButton.button.onClick.AddListener(() =>
                {
                    if (person == null)
                    {
                        return;
                    }

                    Vector3 position = person.mainController.transform.position;
                    Quaternion rotation = person.mainController.transform.rotation;

                    person.LoadPhysicalPresetDialog();

                    person.mainController.transform.SetPositionAndRotation(position, rotation);
                });

                UIDynamicButton savePoseButton = CreateButton("Save Pose");
                savePoseButton.transform.localPosition = new Vector3(-360, -150, 0);
                savePoseButton.button.onClick.AddListener(() =>
                {
                    if (person == null)
                    {
                        return;
                    }

                    person.SavePresetDialog(true, false);
                });
                #endregion
                #endregion

                timeSlider.slider.onValueChanged.AddListener((value) =>
                {

                });

                UIDynamicToggle snapToggle = ui.CreateToggle("Snap To Keyframe", 340, 60);
                snapToggle.transform.localPosition = new Vector3(-270, -30);

                EventTrigger timeSliderET = timeSlider.slider.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.Drag;
                entry.callback.AddListener((data) =>
                {
                    if (keyframes.Count == 0)
                    {
                        return;
                    }

                    if (snapToggle.toggle.isOn)
                    {
                        Keyframe nearestToTime = null;
                        keyframes.ForEach((key) =>
                        {
                            float timeDelta = Mathf.Abs(key.step.timeStep - time.val);
                            if (timeDelta <= 0.01f && nearestToTime == null)
                            {
                                nearestToTime = key;
                            }
                        });

                        if (nearestToTime != null)
                        {
                            SuperController.singleton.SelectController(nearestToTime.step.containingAtom.mainController);
                        }
                        else
                        {
                            SuperController.singleton.ClearSelection();
                            ClearEditingKey();
                        }
                    }
                    else
                    {
                        ClearEditingKey();
                    }


                });
                timeSliderET.triggers.Add(entry);

                Transform thickTimelineTransform = new GameObject().transform;
                thickTimelineTransform.SetParent(animationPattern.containingAtom.mainController.transform, false);
                thickTimeline = thickTimelineTransform.gameObject.AddComponent<LineRenderer>();
                thickTimeline.positionCount = 0;
                thickTimeline.startWidth = 0.02f;
                thickTimeline.endWidth = 0.02f;
                thickTimeline.material = animationPattern.rootLineDrawerMaterial;
                thickTimeline.material.color = new Color(0.15f, 0.66f, 0.0f);
                thickTimeline.useWorldSpace = false;

                updateKeyframeLoop = StartCoroutine(UpdateEditingKeyframeLoop());

                lookAtCamera = new JSONStorableBool("UILookAtCamera", true, (bool on)=>
                {
                    ui.lookAtCamera = on;
                    keyUI.lookAtCamera = on;
                });
                RegisterBool(lookAtCamera);
                CreateToggle(lookAtCamera);

                person.freeControllers.ToList().ForEach((fc) =>
                {
                    JSONStorableBool animateControl = new JSONStorableBool(fc.name, true);
                    RegisterBool(animateControl);
                    CreateToggle(animateControl, true);
                });

                CreateButton("Reverse Animation").button.onClick.AddListener(() =>
                {
                    List<AnimationStep> reversedSteps = animationPattern.steps.ToList();
                    reversedSteps.Reverse();

                    animationPattern.steps = reversedSteps.ToArray();
                    animationPattern.RecalculateTimeSteps();

                    keyframes.Reverse();
                });
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Start()
        {
            if(saveStore.val == saveStore.defaultVal)
            {
                saveJSON = new JSONClass();
                saveJSON["keyframes"] = new JSONArray();
                animationPattern.DestroyAllSteps();
                return;
            }

            saveJSON = JSON.Parse(saveStore.val).AsObject;

            int index = 0;
            List<AnimationStep> accountedFor = new List<AnimationStep>();

            JSONArray keyframes = saveJSON["keyframes"].AsArray;
            for(int i=0; i < keyframes.Count; i++)
            {
                JSONClass pose = keyframes[i].AsObject;
                if (index < animationPattern.steps.Length)
                {
                    AnimationStep step = animationPattern.steps[index];
                    if (step != null)
                    {
                        CreateKeyframe(pose, step);
                        accountedFor.Add(step);
                    }
                }
                else
                {
                    CreateKeyframe(pose);
                }

                index++;
            }

            List<AnimationStep> existingSteps = animationPattern.steps.ToList();
            existingSteps.ForEach((step) =>
            {
                if (accountedFor.Contains(step) == false)
                {
                    step.DestroyStep();
                }
            });

            if (SuperController.singleton.GetSelectedAtom() != containingAtom)
            {
                uiVisible.SetVal(false);
                keyUI.canvas.enabled = false;
                ui.canvas.enabled = false;
            }
        }

        void Update()
        {
            if (animationPattern == null || person == null)
            {
                return;
            }

            CheckStepRemoved();
            EnsureStepsValid();

            if (uiVisible.val)
            {
                ui.Update();
                keyUI.Update();
                UpdateKeyUIVisibility();
            }

            UpdateThickTimeline();

            if (SuperController.singleton.GetSelectedAtom() == animationPattern.containingAtom)
            {
                uiVisible.SetVal(true);
            }

            if (animationPattern.GetBoolParamValue("on"))
            {
                CheckAnimationCompleted();
                UpdateSelection();
                UpdateTimelineSlider();
                CheckStepAdded();
                UpdateAnimationPose();
            }

            if (lookAtCamera.val == true)
            {
                LayoutKeys();
            }
        }

        void OnKeyframeSelected(Keyframe keyframe)
        {
            animationPattern.Pause();
            selectedKeyframe = keyframe;

            keyUI.canvas.enabled = true;
            keyUI.canvas.transform.position = keyframe.step.containingAtom.mainController.transform.position;

            editingKeyframe = keyframe;
            lastPose = editingKeyframe.pose;

            time.val = keyframe.step.timeStep;

            SetToPose(keyframe.pose, true);

            keyUI.canvas.gameObject.SetActive(true);
            SuperController.singleton.PauseSimulation(5, "...");
        }

        void OnKeyframeDeselected(Keyframe keyframe)
        {

        }

        void LateUpdate()
        {
            UpdateKeyframeController();
        }

        void OnDestroy()
        {
            if (ui != null)
            {
                ui.OnDestroy();
            }
            if (keyUI != null)
            {
                keyUI.OnDestroy();
            }
            if (updateKeyframeLoop!=null)
            {
                StopCoroutine(updateKeyframeLoop);
            }

            if (thickTimeline != null)
            {
                Destroy(thickTimeline.gameObject);
            }
        }

        #region Implementation
        void LayoutKeys()
        {
            int index = 0;

            if (lookAtCamera.val == true)
            {
                containingAtom.mainController.transform.LookAt(SuperController.singleton.lookCamera.transform);
            }

            keyframes.ForEach((key) =>
            {
                index++;
                FreeControllerV3 stepMC = key.step.containingAtom.mainController;
                Vector3 position = animationPattern.containingAtom.mainController.transform.position;
                //Vector3 eulerAngles = animationPattern.containingAtom.mainController.transform.eulerAngles;
                //stepMC.transform.position = position;
                //stepMC.transform.eulerAngles = eulerAngles;
                //stepMC.transform.localEulerAngles = Vector3.zero;
                //stepMC.transform.localPosition = new Vector3(0, 0, 0);
                stepMC.transform.position = animationPattern.containingAtom.mainController.transform.TransformPoint(new Vector3(-index * 0.2f, 0, 0));
            });
        }

        void EnsureStepsValid()
        {
            bool changed = false;
            keyframes.ForEach((key) =>
            {
                if (key.step.transitionToTime <= 0.001f)
                {
                    key.step.transitionToTime = 0.001f;
                    changed = true;
                }
            });

            if (changed)
            {
                animationPattern.RecalculateTimeSteps();
            }
        }

        void UpdateThickTimeline()
        {
            thickTimeline.gameObject.SetActive(SuperController.singleton.editModeToggle.isOn);

            if (keyframes.Count <= 1)
            {
                return;
            }

            Transform main = animationPattern.containingAtom.mainController.transform;

            List<Vector3> positions = new List<Vector3>();
            int index = 0;
            keyframes.ForEach((key) =>
            {
                if (time.val >= key.step.timeStep)
                {
                    Vector3 p = main.InverseTransformPoint(key.step.containingAtom.mainController.transform.position);
                    positions.Add(p);
                    index++;
                }
            });

            int nextIndex = index;
            if (nextIndex < keyframes.Count)
            {
                Keyframe nextKeyframe = keyframes[nextIndex];
                float timeDelta = nextKeyframe.step.timeStep - time.val;
                float alpha = 1.0f - (timeDelta / nextKeyframe.step.transitionToTime);
                Vector3 partialEnd = Vector3.Lerp(positions[positions.Count - 1], main.InverseTransformPoint(nextKeyframe.step.containingAtom.mainController.transform.position), alpha);
                positions.Add(partialEnd);
            }

            thickTimeline.SetPositions(positions.ToArray());
            thickTimeline.positionCount = positions.Count;

        }

        void UpdateTimelineSlider()
        {
            //  not sure why it gets reset on load
            if (time.slider != timeSlider.slider)
            {
                time.slider = timeSlider.slider;
            }
            timeSlider.label = "Timeline (" + person.name + ")";
        }

        void UpdateKeyUIVisibility()
        {
            if (editingKeyframe != null)
            {
                keyUI.canvas.transform.position = editingKeyframe.step.containingAtom.mainController.transform.position;
                if (lookAtCamera.val == false)
                {
                    keyUI.canvas.transform.rotation = editingKeyframe.step.containingAtom.mainController.transform.rotation;
                }
            }
            keyUI.canvas.enabled = editingKeyframe != null && keyUI.canvas.enabled;
        }

        IEnumerator UpdateEditingKeyframeLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);
                if (person == null)
                {
                    continue;
                }

                if (editingKeyframe == null)
                {
                    continue;
                }

                if (animationPattern.containingAtom.on == false)
                {
                    continue;
                }

                if (animationPattern.GetBoolParamValue("on") == false)
                {
                    continue;
                }

                editingKeyframe.pose = GetPoseFromPerson();
                UpdateSaveStore();
            }
        }

        void UpdateSelection()
        {
            lastSelectedKeyframe = selectedKeyframe;

            if (selectedKeyframe != null)
            {
                selectedKeyframe.step.GetFloatJSONParam("transitionToTime").sliderAlt = null;
                selectedKeyframe.step.GetStringChooserJSONParam("curveType").popupAlt = null;
                selectedKeyframe = null;
            }

            keyframes.ForEach((key) =>
            {
                if (key.step.containingAtom.mainController.selected)
                {
                    selectedKeyframe = key;

                    key.step.GetFloatJSONParam("transitionToTime").sliderAlt = setDurationSlider.slider;
                    key.step.GetStringChooserJSONParam("curveType").popupAlt = tweenSelection.popup;
                }
            });

            if (lastSelectedKeyframe != selectedKeyframe)
            {
                if (selectedKeyframe != null)
                {
                    OnKeyframeSelected(selectedKeyframe);
                }
                else
                {
                    OnKeyframeDeselected(lastSelectedKeyframe);
                }

                lastSelectedKeyframe = selectedKeyframe;
            }
        }

        void CheckAnimationCompleted()
        {
            if (time.val >= animationPattern.GetTotalTime())
            {
                animationPattern.Pause();
            }
        }

        void ClearEditingKey()
        {
            editingKeyframe = null;
            keyUI.canvas.gameObject.SetActive(false);
        }

        void AddKey()
        {
            JSONClass pose = GetPoseFromPerson();

            Keyframe newKey = CreateKeyframe(pose);

            UpdateSaveStore();

            time.SetVal(animationPattern.GetTotalTime());

            SuperController.singleton.SelectController(newKey.step.containingAtom.mainController);
        }

        void InsertKey()
        {
            if (keyframes.Count <= 1)
            {
                AddKey();
                return;
            }

            float currentTime = time.val;

            JSONClass pose = GetPoseFromPerson();

            int index = GetLastFrameIndex(currentTime);
            Keyframe last = keyframes[index];

            int nextIndex = index + 1;
            if (nextIndex >= keyframes.Count)
            {
                nextIndex = 0;
            }

            Keyframe next = keyframes[nextIndex];
            float nextTimeStep = next.step.timeStep;

            AnimationStep newStep = animationPattern.CreateStepAfterStep(last.step);
            newStep.transitionToTime = currentTime - last.step.timeStep;

            next.step.transitionToTime = nextTimeStep - currentTime;

            Keyframe newKey = CreateKeyframe(pose, newStep, nextIndex);

            UpdateSaveStore();

            time.SetVal(animationPattern.GetTotalTime());

            SuperController.singleton.SelectController(newKey.step.containingAtom.mainController);
        }

        Keyframe CreateKeyframe(JSONClass pose, AnimationStep step = null, int insertionIndex = -1)
        {
            if (step == null)
            {
                step = animationPattern.CreateStepAtPosition(animationPattern.steps.Length);
            }

            step.containingAtom.GetStorableByID("scale").SetFloatParamValue("scale", 0);
            step.containingAtom.mainController.GetComponent<SphereCollider>().radius = 0.06f;


            Keyframe k = new Keyframe(pose, step);
            if (insertionIndex >= 0)
            {
                keyframes.Insert(insertionIndex, k);
            }
            else
            {
                keyframes.Add(k);
            }
            lastCount = keyframes.Count;

            FreeControllerV3 stepMC = step.containingAtom.mainController;
            stepMC.canGrabPosition = false;
            stepMC.canGrabRotation = false;

            LayoutKeys();

            return k;
        }

        Keyframe FindKeyframeByStep(AnimationStep step)
        {
            return keyframes.Find((k) =>
            {
                return k.step == step;
            });
        }

        void UpdateAnimationPose()
        {
            if (animationPattern == null || person == null)
            {
                return;
            }

            float patternTime = animationPattern.GetCurrentTimeCounter();

            if (animationPattern.GetBoolParamValue("pause") == false)
            {
                SetPoseFromTime(patternTime);
                return;
            }

            if (lastPatternTime != animationPattern.GetCurrentTimeCounter())
            {
                SetPoseFromTime(patternTime);
                lastPatternTime = patternTime;
                return;
            }
        }

        int lastCount = 0;
        float lastPatternTime = 0;

        void CheckStepRemoved()
        {
            //  if a pattern was removed
            if (animationPattern.steps.Length < lastCount)
            {
                keyframes = keyframes.Where((k) =>
                {
                    if (k == editingKeyframe)
                    {
                        ClearEditingKey();
                    }
                    return k.step != null;
                }).ToList();
                lastCount = animationPattern.steps.Length;
                //playback.max = animationPattern.GetTotalTime();

                LayoutKeys();

                UpdateSaveStore();
            }
        }

        void UpdateSaveStore()
        {
            JSONArray freshArray = new JSONArray();
            saveJSON["keyframes"] = freshArray;

            keyframes.ForEach((key) =>
            {
                freshArray.Add(key.pose);
            });

            saveStore.SetVal(saveJSON.ToString());
        }

        void CheckStepAdded()
        {
            //  if a pattern was added
            if (animationPattern.steps.Length > lastCount)
            {

            }
        }

        int GetLastFrameIndex(float time)
        {
            if (time >= keyframes[keyframes.Count - 1].step.timeStep && animationPattern.loop)
            {
                return keyframes.Count - 1;
            }

            int indexAfter = keyframes.FindIndex((k) =>
            {
                return k.step.timeStep >= time;
            });

            return Mathf.Clamp(indexAfter - 1, 0, keyframes.Count - 1);
        }

        void SetPoseFromTime(float time)
        {
            if (keyframes.Count <= 0)
            {
                return;
            }

            int lastFrameIndex = GetLastFrameIndex(time);
            int nextFrameIndex = Mathf.Clamp(lastFrameIndex + 1, 0, keyframes.Count - 1);

            bool atLoopPoint = (lastFrameIndex + 1) >= keyframes.Count && animationPattern.loop;
            if (atLoopPoint)
            {
                nextFrameIndex = 0;
            }

            Keyframe lastKeyframe = keyframes[lastFrameIndex];
            Keyframe nextKeyframe = keyframes[nextFrameIndex];

            if (lastKeyframe == nextKeyframe)
            {
                SetToPose(lastKeyframe.pose);
                return;
            }

            float timeToNext = nextKeyframe.step.timeStep - lastKeyframe.step.timeStep;
            if (atLoopPoint)
            {
                timeToNext = nextKeyframe.step.transitionToTime;
            }

            float elapsed = time - lastKeyframe.step.timeStep;
            float alpha = elapsed / timeToNext;
            //Debug.Log(alpha);

            JSONStorableStringChooser curveType = nextKeyframe.step.GetStringChooserJSONParam("curveType");
            bool easing = curveType.val != curveType.defaultVal;
            TweenTransform(lastKeyframe.pose, nextKeyframe.pose, alpha, easing);
        }

        public void SetToPose(JSONNode pose, bool setBones = false)
        {
            TweenTransform(pose, pose, 0, false, setBones);
        }

        public JSONClass GetPoseFromPerson()
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

        public void TweenTransform(JSONNode from, JSONNode to, float alpha, bool easing = false, bool transformBones = false)
        {
            alpha = Mathf.Clamp01(alpha);

            if (easing)
            {
                alpha = CubicEaseInOut(alpha);
            }

            Transform personRoot = person.mainController.transform;

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
                if(isBone && transformBones == false)
                {
                    continue;
                }

                if (!isBone)
                {
                    if (GetBoolParamValue(id) == false)
                    {
                        continue;
                    }
                }

                JSONStorable storable = person.GetStorableByID(id);

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

        private List<string> GetPeopleNamesFromScene()
        {
            return SuperController.singleton.GetAtoms()
                    .Where(atom => atom.GetStorableByID("geometry") != null && atom != containingAtom)
                    .Select(atom => atom.name).ToList();
        }

        private JSONStorableStringChooser CreatePeopleChooser(Action<Atom> onChange)
        {
            List<string> people = GetPeopleNamesFromScene();
            JSONStorableStringChooser personChoice = new JSONStorableStringChooser("copyFrom", people, null, "Copy From", (string id) =>
            {
                Atom atom = GetAtomById(id);
                onChange(atom);
            })
            {
                storeType = JSONStorableParam.StoreType.Full
            };

            if (people.Count > 0)
            {
                personChoice.SetVal(people[0]);
            }

            UIDynamicPopup scenePersonChooser = CreateScrollablePopup(personChoice, false);
            scenePersonChooser.popupPanelHeight = 250f;
            RegisterStringChooser(personChoice);
            scenePersonChooser.popup.onOpenPopupHandlers += () =>
            {
                personChoice.choices = GetPeopleNamesFromScene();
            };

            return personChoice;
        }

        private void UpdateKeyframeController()
        {
            float time = animationPattern.GetCurrentTimeCounter();
            keyframes.ForEach((key) =>
            {
                FreeControllerV3 mc = key.step.containingAtom.mainController;
                if (time >= key.step.timeStep)
                {
                    mc.deselectedMeshScale = 0.012f;
                }
                else
                {
                    mc.deselectedMeshScale = 0.007f;
                }

            });
        }

        private Keyframe GetNearestKeyToTime(float time)
        {
            if (keyframes.Count <= 0)
            {
                return null;
            }

            float smallestTimeDelta = 10000000.0f;
            Keyframe nearest = keyframes[0];
            keyframes.ForEach((key) =>
            {
                float timeDelta = Mathf.Abs(time - key.step.timeStep);
                if (timeDelta < smallestTimeDelta)
                {
                    smallestTimeDelta = timeDelta;
                    nearest = key;
                }
            });

            return nearest;
        }
        #endregion
    }

}