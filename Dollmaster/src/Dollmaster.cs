using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Dollmaster
{
    public class DollmasterPlugin : MVRScript
    {
        public static string PLUGIN_PATH;
        public static string ASSETS_PATH;
        public static string LOAD_PATH;

        public Personas personas;
        public Personality personality;
        public PluginState ps;
        public UI ui;

        public List<BaseModule> modules = new List<BaseModule>();

        public Config config;
        public Expressions expressions;
        public ExpressionController expressionController;
        public ThrustController thrustController;
        public HeadController headController;
        public Arousal arousal;
        public BreathController breathController;
        public MontageController montageController;
        public PoseController poseController;
        public DressController dressController;
        public ClimaxController climaxController;
        public KissController kissController;
        public SFXController sfxController;

        public AudioSourceControl headAudioSource;

        string lastAtomName = "";

        public override void Init()
        {
            try
            {
                if (containingAtom.type != "Person")
                {
                    SuperController.LogError("Please add Doll Master to a Person atom");
                    return;
                }

                lastAtomName = containingAtom.uid;

                PLUGIN_PATH = GetPluginPath();
                ASSETS_PATH = PLUGIN_PATH + "/Assets";
                LOAD_PATH = SuperController.singleton.currentLoadDir;

                RegisterActions();

                ui = new UI(this, 0.001f);
                ui.canvas.transform.Translate(0, 0.2f, 0);

                config = new Config(this);

                personas = new Personas(this);

                ps = new PluginState(this);

                arousal = new Arousal(this);

                expressions = new Expressions(this);

                expressionController = new ExpressionController(this);

                sfxController = new SFXController(this);

                thrustController = new ThrustController(this);

                breathController = new BreathController(this);

                headController = new HeadController(this);

                poseController = new PoseController(this);

                montageController = new MontageController(this, poseController);

                dressController = new DressController(this);

                climaxController = new ClimaxController(this);

                kissController = new KissController(this);

                new TopButtons(this);

                new WorldUI(this);

                headAudioSource = containingAtom.GetStorableByID("HeadAudioSource") as AudioSourceControl;

                CreateSpacer().height = 200;

                //SuperController singleton = SuperController.singleton;
                //singleton.onAtomUIDRenameHandlers = (SuperController.OnAtomUIDRename)Delegate.Combine(singleton.onAtomUIDRenameHandlers, new SuperController.OnAtomUIDRename(HandleRename));

            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }

        }

        void RegisterActions()
        {
            JSONStorableAction emotionTrigger = new JSONStorableAction("emotionTrigger", () =>
            {
                ForcePlayExpression();
            });
            RegisterAction(emotionTrigger);

            CreateButton("Test Emotion Trigger", true).button.onClick.AddListener(() =>
            {
                emotionTrigger.actionCallback();
            });

            JSONStorableAction climaxTrigger = new JSONStorableAction("climaxTrigger", () =>
            {
                arousal.MaxOut();
            });
            RegisterAction(climaxTrigger);

            CreateButton("Test Climax Trigger", true).button.onClick.AddListener(() =>
            {
                climaxTrigger.actionCallback();
            });

            CreateSpacer(true);
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass jSON = base.GetJSON(includePhysical, includeAppearance);
            jSON["montages"] = montageController.GetJSON();
            return jSON;
        }

        public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms);

            if (jc["montages"] != null)
            {
                montageController.Load(jc["montages"].AsArray);
            }

            dressController.OnRestore();
        }

        public override void PostRestore()
        {
            base.PostRestore();
            montageController.PostRestore();
            config.Apply();
        }

        void Start()
        {
            if(ps.personalityChoice.val != ps.personalityChoice.defaultVal)
            {
                personality = personas.GetPersonality(ps.personalityChoice.val);
            }
            else
            {
                ps.personalityChoice.SetVal(personas.GetRandomPersonality().name);
            }
        }

        void Update()
        {
            try
            {
                if (ui != null)
                {
                    ui.Update();
                }

                modules.ForEach((module) =>
                {
                    module.Update();
                });

                //  name change handlers are breaking for some reason
                //  hack around this...
                if (lastAtomName != containingAtom.uid)
                {
                    HandleRename(lastAtomName, containingAtom.uid);
                    lastAtomName = containingAtom.uid;
                }
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void OnDestroy()
        {
            try
            {
                if (ui != null)
                {
                    ui.OnDestroy();
                }

                modules.ForEach((module) =>
                {
                    module.OnDestroy();
                });

                if (personality != null)
                {
                    personality.OnDestroy();
                }

                //SuperController singleton = SuperController.singleton;
                //singleton.onAtomUIDRenameHandlers = (SuperController.OnAtomUIDRename)Delegate.Remove(singleton.onAtomUIDRenameHandlers, new SuperController.OnAtomUIDRename(HandleRename));

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }

        }

        public void SetPersonality(string name)
        {
            if (personality != null)
            {
                personality.Unload();
            }
            personality = personas.GetPersonality(name);
            personality.Load();
        }

        public void PlayAudio(NamedAudioClip clip)
        {
            if (clip != null)
            {
                headAudioSource.PlayNow(clip);
            }
        }

        public void TriggerExpression()
        {
            sfxController.Trigger();

            if (climaxController.isClimaxing || climaxController.isResting)
            {
                return;
            }

            if (thrustController.sliderValue <= .2f)
            {
                return;
            }

            arousal.Trigger();
            headController.Trigger();
            expressionController.Trigger();
            poseController.Trigger();
        }

        public void ForcePlayExpression()
        {
            arousal.Trigger();
            expressionController.StartExpression();
            poseController.Trigger();
        }

        public void TriggerClimax()
        {
            expressionController.TriggerClimax();
            poseController.SelectRandomPose();
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

        public static NamedAudioClip LoadAudio(string path)
        {
            string localPath = SuperController.singleton.NormalizeLoadPath(path);
            NamedAudioClip existing = URLAudioClipManager.singleton.GetClip(localPath);
            if (existing != null)
            {
                return existing;
            }

            URLAudioClip clip = URLAudioClipManager.singleton.QueueClip(SuperController.singleton.NormalizeMediaPath(path));
            if (clip == null)
            {
                return null;
            }

            NamedAudioClip nac = URLAudioClipManager.singleton.GetClip(clip.uid);
            if (nac == null)
            {
                return null;
            }
            return nac;
        }

        protected void HandleRename(string oldId, string newId)
        {
            modules.ForEach((module) =>
            {
                module.HandleRename(oldId, newId);
            });
        }
    }
}
