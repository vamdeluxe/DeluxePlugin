using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin
{
    public class Activity : MVRScript
    {
        public StateMachine hipState;
        public Idle idle;
        public Sex sex;
        public Climax climax;
        public Refractory refractory;

        HipThrust hipAction;
        Breathe breatheAction;

        public float intensity = 0.0f;
        public float desiredIntensity = 0.0f;
        public float maxIntensity = 1.0f;
        public float intensityDecay = 0.1f;

        public bool inserted = true;

        public EmotionEngine emotionEngine;
        public Gaze gaze;
        public OMeter oMeter;

        public override void Init()
        {
            try
            {
                emotionEngine = Utils.GetPluginStorableById(containingAtom, "EmotionEngine") as EmotionEngine;

                hipAction = Utils.GetPluginStorableById(containingAtom, "HipThrust") as HipThrust;

                breatheAction = Utils.GetPluginStorableById(containingAtom, "Breathe") as Breathe;

                gaze = Utils.GetPluginStorableById(containingAtom, "Gaze") as Gaze;

                oMeter = Utils.GetPluginStorableById(containingAtom, "OMeter") as OMeter;

                gaze.SetReference(containingAtom.name, "eyeTargetControl");
                gaze.SetLookAtPlayer(-0.10f * Vector3.up);
                gaze.SetGazeDuration(0.2f);

                idle = new Idle(this);
                sex = new Sex(this);
                climax = new Climax(this);
                refractory = new Refractory(this);

                hipState = new StateMachine();

                JSONStorableString debugState = new JSONStorableString("debugState", "");
                CreateTextField(debugState);

                hipState.onStateChanged += (State previous, State next) =>
                {
                    debugState.val = next.ToString();
                };

                hipState.Switch(idle);

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        public void Update()
        {
            hipState.OnUpdate();
            breatheAction.intensity.val = intensity;
            oMeter.SetFloatParamValue("<3", intensity);
        }

        public void FixedUpdate()
        {
            gaze.OnFixedUpdate();
        }

        #region States

        #region ActivityState
        public class ActivityState : State
        {
            protected Activity activity;
            public ActivityState(Activity activity) : base()
            {
                this.activity = activity;
            }
        }
        #endregion

        #region Idle
        public class Idle : ActivityState
        {
            public Idle(Activity activity) : base(activity) { }
            public override void OnEnter()
            {
                Duration = 2.6f;
                activity.emotionEngine.StartBreathing(activity.breatheAction, Duration);
                activity.emotionEngine.eyelidControl.SetBoolParamValue("blinkEnabled", true);
            }
            public override void OnUpdate()
            {
                if ( activity.hipAction.GetHipIntensity() > 0.01f)
                {
                    stateMachine.Switch(activity.sex);
                }

                activity.intensity -= activity.intensityDecay * 0.4f * 1.0f / (1.4f - activity.intensity) * Time.deltaTime;
                activity.intensity = Mathf.Clamp(activity.intensity, 0, activity.maxIntensity);
            }
            public override void OnTimeout()
            {
                stateMachine.Switch(activity.idle);
            }

        }
        #endregion

        #region Sex
        public class Sex : ActivityState
        {
            public Sex(Activity activity) : base(activity) { }

            public override void OnEnter()
            {
                activity.gaze.SetLookAtPlayer(-1.4f * Vector3.up);
                activity.gaze.SetGazeDuration(0.2f);
                EmotionEngine em = activity.emotionEngine;
                em.panting.Stop();
                em.breathingActive.Stop();
                em.breathingIdle.Stop();
                activity.emotionEngine.eyelidControl.SetBoolParamValue("blinkEnabled", false);
            }

            public override void OnUpdate()
            {
                UpdateIntensity(activity.inserted, activity.hipAction);

                EmotionEngine em = activity.emotionEngine;
                if (em.CanPlay())
                {
                    em.SelectExpression(activity.intensity, activity.maxIntensity, activity.desiredIntensity);
                }

                if (activity.intensity >= (activity.maxIntensity-0.005f))
                {
                    stateMachine.Switch(activity.climax);
                }

                if(activity.intensity <= 0.8f && activity.hipAction.GetHipIntensity() < 0.01f)
                {
                    stateMachine.Switch(activity.idle);
                }

                bool headSelected = false;

                FreeControllerV3 control = activity.containingAtom.GetStorableByID("headControl") as FreeControllerV3;
                if (control != null && control.currentPositionState == FreeControllerV3.PositionState.ParentLink)
                {
                    headSelected = true;
                }


                if (headSelected == false)
                {
                    activity.gaze.SetReference(activity.containingAtom.name, "eyeTargetControl");
                    activity.gaze.SetLookAtPlayer(-0.1f * Vector3.up);
                    activity.gaze.SetGazeDuration(0.3f);
                }
            }


            private void UpdateIntensity(bool inserted, HipThrust hipAction)
            {
                float hipIntensity = hipAction.GetHipIntensity() * 0.08f;

                float addIntensity = 0.0f;

                if (hipIntensity > 0)
                {
                    addIntensity += hipIntensity;
                    addIntensity -= activity.intensityDecay * 0.2f * 1.0f / (1.4f - activity.intensity);
                }
                else
                {
                    addIntensity -= activity.intensityDecay * 1.5f;
                }

                activity.desiredIntensity = hipIntensity * 10.0f;

                activity.intensity += addIntensity * Time.deltaTime;
                activity.intensity = Mathf.Clamp(activity.intensity, 0, activity.maxIntensity);
            }


        }
        #endregion

        #region Climax
        public class Climax : ActivityState
        {
            public float climaxDuration = 10.0f;
            public Climax(Activity activity) : base(activity) { }
            public override void OnEnter()
            {
                Duration = climaxDuration;
                activity.emotionEngine.SelectClimax(climaxDuration);
                //activity.actor.gazeController.SetLookAtPlayer(2.5f * Vector3.up);
                //activity.actor.gazeController.SetGazeDuration(4.0f);

            }

            public override void OnTimeout()
            {
                stateMachine.Switch(activity.refractory);
            }
        }
        #endregion

        #region Refractory
        public class Refractory : ActivityState
        {
            public float refractoryDuration = 6.0f;
            public Refractory(Activity activity) : base(activity) {}

            public override void OnEnter()
            {
                Duration = refractoryDuration;
                activity.emotionEngine.StartPanting(refractoryDuration);
                activity.gaze.SetLookAtPlayer(-0.1f * Vector3.up);
                activity.gaze.SetGazeDuration(1.0f);
            }

            public override void OnUpdate()
            {
                activity.intensity -= 0.1f * Time.deltaTime;
                activity.intensity = Mathf.Clamp(activity.intensity, 0, activity.maxIntensity);
            }

            public override void OnTimeout()
            {
                stateMachine.Switch(activity.sex);
            }
        }
        #endregion

        #endregion

    }
}
