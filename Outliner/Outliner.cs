using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Outliner
{
    public class Outliner : MVRScript
    {
        UI ui;
        RectTransform group;

        string filter = "All";

        Color filterDeselectedColor = new Color(0.25f, 0.25f, 0.25f);
        Color filterSelectedColor = new Color(0.3f, 0.45f, 0.7f);

        Color atomDeselectedColor = new Color(0.2f, 0.2f, 0.2f);
        Color atomSelectedColor = new Color(0.3f, 0.45f, 0.7f);

        Atom parentAtom;

        Dictionary<string, List<string>> filters = new Dictionary<string, List<string>>()
        {
            { "All", new List<string>(){ "All" } },
            { "People", new List<string>(){ "Person" } },
            { "Lights", new List<string>(){ "InvisibleLight" } },
            { "Animation Pattern", new List<string>(){ "AnimationPattern" } },
            { "Audio", new List<string>(){"AudioSource", "RhythmAudioSource"} },
            { "Triggers", new List<string>(){"Button", "CollisionTrigger", "LookAtTrigger", "UIButton", "VariableTrigger"} },
        };

        Dictionary<Atom, ControlSetup> atomNameToControlSetup = new Dictionary<Atom, ControlSetup>();

        Dictionary<string, UIDynamicButton> filterToButton = new Dictionary<string, UIDynamicButton>();


        class ControlSetup
        {
            public UIDynamicButton selectButton;
            public UIDynamicToggle onToggle;
            public UIDynamicToggle hideToggle;
        }

        public override void Init()
        {
            try
            {
                Setup();
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Setup()
        {
            if (containingAtom.mainController == null)
            {
                SuperController.LogError("Please add this plugin to an atom, such as Misc->Empty.");
                return;
            }

            parentAtom = containingAtom;
            parentAtom.mainController.interactableInPlayMode = true;

            ui = new UI(this, 0.0015f);
            ui.canvas.transform.SetParent(parentAtom.mainController.transform, false);
            ui.canvas.transform.localPosition = new Vector3(-0.06f, -.15f, 0);

            group = ui.CreateScrollRect(300, 80);
            group.transform.Translate(0, -0.15f, 0, Space.Self);

            CreateFilters();

            SuperController.singleton.onAtomUIDRenameHandlers += HandleRename;

            lastEditMode = SuperController.singleton.editModeToggle;

            lastAtomsList = SuperController.singleton.GetAtoms();
            RefreshUI();
        }

        void CreateFilters()
        {
            RectTransform filterGroup = ui.CreateHorizontalLayout(1000, 80);
            filterGroup.transform.localPosition = new Vector3(300, 0, 0);
            //filterGroup.SetParent(group, false);

            List<UIDynamicButton> filterButtons = new List<UIDynamicButton>();

            List<string> filterNames = filters.Keys.ToList();
            filterButtons = filterNames.Select((filterName) =>
            {
                UIDynamicButton button = ui.CreateButton(filterName, 200, 80);
                button.transform.SetParent(filterGroup, false);

                button.buttonText.resizeTextForBestFit = true;
                button.buttonText.resizeTextMaxSize = 28;

                button.buttonColor = filterDeselectedColor;

                button.button.onClick.AddListener(() =>
                {
                    filterButtons.ForEach((otherButton) =>
                    {
                        otherButton.buttonColor = filterDeselectedColor;
                    });

                    button.buttonColor = filterSelectedColor;
                    filter = filterName;
                    RefreshUI();
                });

                button.textColor = new Color(1, 1, 1);

                filterToButton[filterName] = button;

                return button;
            }).ToList();

            filterButtons[0].buttonColor = filterSelectedColor;
        }


        void HandleRename(string old, string newName)
        {
            RefreshUI();
        }

        void RefreshFilters()
        {
            filterToButton.Values.ToList().ForEach((button) =>
            {
                button.button.gameObject.SetActive(false);
            });

            SuperController.singleton.GetAtoms().ForEach((atom) =>
            {
                filterToButton.Keys.ToList().ForEach((filter) =>
                {
                    if (filters[filter].Contains(atom.type))
                    {
                        filterToButton[filter].button.gameObject.SetActive(true);
                    }
                });
            });

            filterToButton["All"].button.gameObject.SetActive(true);
            if (filterToButton[filter].button.IsActive() == false)
            {
                filter = "All";
                filterToButton["All"].buttonColor = filterSelectedColor;
            }
        }

        void RefreshUI()
        {
            RefreshFilters();

            atomNameToControlSetup.Clear();

            List<Transform> children = new List<Transform>();
            for(int i = 0; i < group.childCount; i++)
            {
                children.Add(group.GetChild(i));
            }

            children.ForEach((child) =>
            {
                GameObject.Destroy(child.gameObject);
            });

            group.DetachChildren();

            SuperController.singleton.GetAtoms()
            .Where((atom) =>
            {
                if (filter == "All")
                {
                    return true;
                }

                List<string> accepted = filters[filter];
                return accepted.Contains(atom.type);
            })
            .ToList()
            .ForEach((atom) =>
            {
                if (atom.mainController == null)
                {
                    return;
                }

                if (atom == parentAtom)
                {
                    return;
                }

                Transform atomGroup = ui.CreateHorizontalLayout(1000, 60);
                atomGroup.SetParent(group, false);

                UIDynamicToggle onToggle = ui.CreateToggle("On", 120, 60);
                onToggle.transform.SetParent(atomGroup, false);

                onToggle.toggle.isOn = atom.on;
                onToggle.toggle.onValueChanged.AddListener((on) =>
                {
                    atom.SetOn(on);
                });

                UIDynamicToggle hideToggle = ui.CreateToggle("Hide Controls", 240, 60);
                hideToggle.transform.SetParent(atomGroup, false);

                hideToggle.toggle.isOn = atom.mainController.interactableInPlayMode;
                hideToggle.toggle.onValueChanged.AddListener((on) =>
                {
                    atom.mainController.interactableInPlayMode = on;
                    SetVisibility(atom, !hideToggle.toggle.isOn);
                });

                SetVisibility(atom, !hideToggle.toggle.isOn);

                UIDynamicButton selectButton = ui.CreateButton(atom.uid, 500, 60);
                selectButton.transform.SetParent(atomGroup, false);

                HorizontalLayoutGroup hlg = selectButton.button.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(6, 6, 6, 6);
                //ContentSizeFitter csf = selectButton.button.gameObject.AddComponent<ContentSizeFitter>();
                //csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                selectButton.buttonText.alignment = TextAnchor.MiddleLeft;

                selectButton.button.onClick.AddListener(() =>
                {
                    SuperController.singleton.SelectController(atom.mainController);
                });

                selectButton.textColor = new Color(1, 1, 1);
                selectButton.buttonColor = atomDeselectedColor;

                UIDynamicButton deleteButton = ui.CreateButton("Delete", 120, 60);
                deleteButton.transform.SetParent(atomGroup, false);
                deleteButton.buttonColor = new Color(0.4f, 0, 0);
                deleteButton.textColor = Color.white;
                deleteButton.button.onClick.AddListener(() =>
                {
                    SuperController.singleton.RemoveAtom(atom);
                });
                //hlg2 = deleteButton.button.gameObject.AddComponent<HorizontalLayoutGroup>();
                //hlg2.padding = new RectOffset(6, 6, 6, 6);

                atomNameToControlSetup[atom] = new ControlSetup()
                {
                    selectButton = selectButton,
                    hideToggle = hideToggle,
                    onToggle = onToggle
                };

            });

        }

        Atom lastSelected;

        bool lastEditMode = false;

        List<Atom> lastAtomsList = new List<Atom>();

        void Update()
        {
            if (parentAtom == null)
            {
                return;
            }

            if (ui != null)
            {
                ui.Update();
            }

            if (SuperController.singleton.GetAtoms().Count != lastAtomsList.Count)
            {
                List<Atom> newAtoms = SuperController.singleton.GetAtoms().Where((atom) =>
                {
                    return lastAtomsList.Contains(atom) == false;
                }).ToList();

                newAtoms.ForEach((atom) =>
                {
                    if (atom.mainController != null)
                    {
                        atom.mainController.interactableInPlayMode = false;
                    }
                });

                RefreshUI();
                lastAtomsList = SuperController.singleton.GetAtoms();
            }

            Atom selected = SuperController.singleton.GetSelectedAtom();
            if (selected != null)
            {
                if (atomNameToControlSetup.ContainsKey(selected))
                {
                    atomNameToControlSetup[selected].selectButton.buttonColor = atomSelectedColor;
                }
            }
            if (lastSelected != selected)
            {
                if (lastSelected!=null && atomNameToControlSetup.ContainsKey(lastSelected))
                {
                    atomNameToControlSetup[lastSelected].selectButton.buttonColor = atomDeselectedColor;
                }
                lastSelected = selected;
            }

            if(lastEditMode != SuperController.singleton.editModeToggle.isOn)
            {
                lastEditMode = SuperController.singleton.editModeToggle.isOn;

                if(SuperController.singleton.editModeToggle.isOn)
                {
                    SuperController.singleton.GetAtoms().ForEach((atom) =>
                    {
                        if (atom.mainController == null)
                        {
                            return;
                        }

                        SetVisibility(atom, true);
                    });
                }
                else
                {
                    SuperController.singleton.GetAtoms().ForEach((atom) =>
                    {
                        if (atom.mainController == null)
                        {
                            return;
                        }

                        if (atomNameToControlSetup.ContainsKey(atom) == false)
                        {
                            return;
                        }

                        ControlSetup cs = atomNameToControlSetup[atom];
                        SetVisibility(atom, !cs.hideToggle.toggle.isOn);
                    });
                }
            }
        }

        void OnDestroy()
        {
            if (ui != null)
            {
                ui.OnDestroy();
            }

            SuperController.singleton.onAtomUIDRenameHandlers -= HandleRename;
        }

        void SetVisibility(Atom atom, bool visible)
        {
            if (atom == parentAtom)
            {
                return;
            }

            if (visible)
            {
                //  the default
                //Debug.Log(atom.mainController.deselectedMeshScale);


                atom.freeControllers.ToList().ForEach((controller) =>
                {
                    controller.unhighlightedScale = .2f;
                    controller.deselectedMeshScale = 0.02f;
                    controller.enabled = true;
                    //controller.GetComponents<FreeControllerV3>().ToList().ForEach((component) =>
                    //{
                    //    component.enabled = true;
                    //});
                });
            }
            else
            {
                atom.freeControllers.ToList().ForEach((controller) =>
                {
                    controller.unhighlightedScale = 0.00f;
                    controller.deselectedMeshScale = 0.0f;
                    controller.enabled = false;
                    //controller.GetComponents<FreeControllerV3>().ToList().ForEach((component) =>
                    //{
                    //    component.enabled = false;
                    //});
                });
            }
        }
    }
}


namespace DeluxePlugin.Outliner
{
    public class UI
    {
        public Canvas canvas;

        MVRScript plugin;

        float UIScale = 1;

        public UI(MVRScript plugin, float scale = 0.002f)
        {
            this.plugin = plugin;
            this.UIScale = scale;

            GameObject canvasObject = new GameObject();
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            SuperController.singleton.AddCanvas(canvas);

            CanvasScaler cs = canvasObject.AddComponent<CanvasScaler>();
            cs.scaleFactor = 100.0f;
            cs.dynamicPixelsPerUnit = 1f;

            GraphicRaycaster gr = canvasObject.AddComponent<GraphicRaycaster>();

            canvas.transform.localScale = new Vector3(scale, scale, scale);
        }

        public UIDynamicSlider CreateSlider(string name, float width = 300, float height = 80)
        {
            Transform slider = GameObject.Instantiate<Transform>(plugin.manager.configurableSliderPrefab);
            ConfigureTransform(slider, width, height);

            UIDynamicSlider sliderDynamic = slider.GetComponent<UIDynamicSlider>();
            sliderDynamic.quickButtonsEnabled = false;
            sliderDynamic.rangeAdjustEnabled = false;
            sliderDynamic.defaultButtonEnabled = false;
            sliderDynamic.label = name;

            return sliderDynamic;
        }

        public UIDynamicButton CreateButton(string name, float width = 100, float height = 80)
        {
            Transform button = GameObject.Instantiate<Transform>(plugin.manager.configurableButtonPrefab);
            ConfigureTransform(button, width, height);

            UIDynamicButton uiButton = button.GetComponent<UIDynamicButton>();
            uiButton.label = name;

            return uiButton;
        }

        public UIDynamicToggle CreateToggle(string name, float width = 100, float height = 80)
        {
            Transform toggle = GameObject.Instantiate<Transform>(plugin.manager.configurableTogglePrefab);
            ConfigureTransform(toggle, width, height);

            UIDynamicToggle uiToggle = toggle.GetComponent<UIDynamicToggle>();
            uiToggle.label = name;
            return uiToggle;
        }

        public UIDynamicPopup CreatePopup(string name, float width = 100, float height = 80)
        {
            Transform popup = GameObject.Instantiate<Transform>(plugin.manager.configurablePopupPrefab);
            ConfigureTransform(popup, width, height);

            UIDynamicPopup uiPopup = popup.GetComponent<UIDynamicPopup>();
            uiPopup.label = name;
            return uiPopup;
        }

        public RectTransform CreateHorizontalLayout(float width = 300, float height = 500)
        {
            GameObject go = new GameObject();
            RectTransform rt = go.AddComponent<RectTransform>();
            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            rt.SetParent(canvas.transform, false);
            rt.sizeDelta = new Vector2(width, height);
            //rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            //rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            return rt;
        }

        public RectTransform CreateScrollRect(float width=300, float height=500)
        {
            GameObject vGo = new GameObject();
            vGo.AddComponent<VerticalLayoutGroup>();
            RectTransform vRT = vGo.GetComponent<RectTransform>();
            vRT.SetParent(canvas.transform, false);
            return vRT;
        }

        private void ConfigureTransform(Transform t, float width, float height)
        {
            t.transform.position = Vector3.zero;
            t.SetParent(canvas.transform, false);

            RectTransform rt = t.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            //RectTransform timeSliderRT = t.GetComponent<RectTransform>();
            //timeSliderRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            //timeSliderRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public void Update()
        {
            if (XRSettings.enabled == false)
            {
                Transform cameraT = SuperController.singleton.lookCamera.transform;
                Vector3 endPos = cameraT.position + cameraT.forward * 10000000.0f;
                canvas.transform.LookAt(endPos, cameraT.up);
            }
            else
            {
                canvas.transform.localEulerAngles = new Vector3(0, 180, 0);
            }

            canvas.transform.localScale = Vector3.one * plugin.containingAtom.GetStorableByID("scale").GetFloatParamValue("scale") * UIScale;

            canvas.enabled = SuperController.singleton.editModeToggle.isOn;
        }

        public void OnDestroy()
        {
            SuperController.singleton.RemoveCanvas(canvas);
            GameObject.Destroy(canvas.gameObject);
        }
    }
}
