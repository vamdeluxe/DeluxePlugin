using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.DollMaker
{
    class Appearance : Module
    {

        List<BoneAttachedUI> baUI = new List<BoneAttachedUI>();

        GenerateDAZMorphsControlUI morphControl;
        HashSet<string> regions;

        Dictionary<string, UIDynamicSlider> morphNameToSlider = new Dictionary<string, UIDynamicSlider>();
        InputField searchBox;

        public Appearance(DollMaker dm) : base(dm)
        {

            JSONArray bodyUI = DollMaker.CONFIG_JSON["bodyUI"].AsArray;
            for(int i=0;i< bodyUI.Count; i++)
            {
                JSONClass uiPart = bodyUI[i].AsObject;
                string title = uiPart["title"].Value;
                string boneName = uiPart["bone"].Value;
                string searchTerm = uiPart["search"].Value;
                float x = uiPart["offset"]["x"].AsFloat;
                float y = uiPart["offset"]["y"].AsFloat;
                float z = uiPart["offset"]["z"].AsFloat;

                DAZBone bone = person.GetStorableByID(boneName) as DAZBone;

                BoneAttachedUI boneUI = new BoneAttachedUI(title, bone, ui, new Vector3(x, y, z));
                boneUI.button.button.onClick.AddListener(() =>
                {
                    UpdateSearch(searchTerm);
                    searchBox.text = searchTerm;
                });
                baUI.Add(boneUI);


            }

            searchBox = ui.CreateTextInput("Search For Morph", 1200, 120);
            searchBox.transform.localPosition = new Vector3(0, -120, 0);


            //textInput.gameObject.AddComponent<InputPressedHandler>();

            ScrollRect sr = ui.CreateScrollRect(1200, 800);
            sr.transform.localPosition = new Vector3(0, -600, 0);

            VerticalLayoutGroup vlg = ui.CreateVerticalLayout(1200, 1200);
            vlg.transform.SetParent(sr.transform, false);
            vlg.padding = new RectOffset(0, 0, 25, 25);

            RectTransform vlgrt = vlg.GetComponent<RectTransform>();
            vlgrt.pivot = new Vector2(0.5f, 1);
            vlgrt.anchoredPosition = new Vector2(0, 0);
            sr.content = vlgrt;

            ContentSizeFitter csf = vlg.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            DAZCharacterSelector personGeometry = person.GetStorableByID("geometry") as DAZCharacterSelector;
            morphControl = personGeometry.morphsControlUI;

            regions = new HashSet<string>();
            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                regions.Add(morph.region);
            });

            List<string> morphNames = personGeometry.GetFloatParamNames();
            morphNames.ForEach((s) =>
            {
                //  For testing scroll rect
                //if (morphNameToSlider.Keys.Count > 20)
                //{
                //    return;
                //}

                UIDynamicSlider morphSlider = ui.CreateSlider(s, 800, 80, false);
                morphSlider.gameObject.SetActive(false);

                //Image image = morphSlider.labelText.transform.parent.gameObject.GetComponentInChildren<Image>();
                morphSlider.transform.SetParent(vlg.transform, false);

                personGeometry.GetFloatJSONParam(s).slider = morphSlider.slider;

                morphNameToSlider[s] = morphSlider;
            });

            searchBox.onValueChanged.AddListener(UpdateSearch);
        }

        void UpdateSearch(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                HideSliders();
                return;
            }

            morphNameToSlider.Keys.ToList().ForEach((name) =>
            {
                bool matches = MatchName(name, value);
                morphNameToSlider[name].gameObject.SetActive(matches);
            });
        }


        bool MatchName(string morphName, string searchTerm)
        {
            if(searchTerm.Length < 3)
            {
                return false;
            }

            string searchTermLower = searchTerm.ToLower();
            string morphNameLower = morphName.ToLower();
            if (searchTermLower.Contains(morphNameLower) || morphNameLower.Contains(searchTermLower) )
            {
                return true;
            }

            DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);
            if (morph == null)
            {
                return false;
            }

            string morphRegionLower = morph.region.ToLower();
            if (searchTermLower.Contains(morphRegionLower) || morphRegionLower.Contains(searchTermLower))
            {
                return true;
            }

            return false;
        }

        void HideSliders()
        {
            morphNameToSlider.Values.ToList().ForEach((slider) =>
            {
                slider.gameObject.SetActive(false);
            });
        }

        public class InputPressedHandler : MonoBehaviour, IPointerDownHandler
        {
            public void OnPointerDown(PointerEventData eventData)
            {
                //LookInputModule.singleton.ActivateModule();
                //LookInputModule.singleton.Select(this.gameObject);
            }
        }

        public override void Update()
        {
            baUI.ForEach((ba) =>
            {
                ba.Update();
            });
        }

        class BoneAttachedUI
        {
            public UIDynamicButton button;
            DAZBone bone;
            Vector3 offset;
            public BoneAttachedUI(string title, DAZBone bone, UI ui, Vector3 offset)
            {
                this.bone = bone;
                button = ConfigButton( ui.CreateButton(title.ToUpper(), 400, 80) );
                this.offset = offset;
            }

            UIDynamicButton ConfigButton(UIDynamicButton button)
            {
                button.buttonColor = DollMaker.BG_COLOR;
                button.textColor = DollMaker.FG_COLOR;
                button.buttonText.fontSize = 40;
                return button;
            }

            public void Update()
            {
                button.transform.position = bone.transform.position + bone.containingAtom.transform.TransformPoint(offset);
            }
        }
    }
}
