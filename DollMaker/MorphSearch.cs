using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.DollMaker
{
    class MorphSearch
    {
        public InputField searchBox;

        GenerateDAZMorphsControlUI morphControl;
        HashSet<string> regions;

        const int MAX_MORPHS_PER_PAGE = 25;
        const int MAX_TERMS = 16;
        bool switchSort = false;

        List<UIDynamicSlider> morphSliders = new List<UIDynamicSlider>();
        List<string> morphNames = new List<string>();

        UIDynamicSlider paginationSlider;
        JSONStorableFloat paginationValue;

        List<string> commonTerms = new List<string>();
        List<UIDynamicButton> termButtons = new List<UIDynamicButton>();

        public MorphSearch(BaseModule baseModule)
        {
            UI ui = baseModule.ui;
            Transform moduleUI = baseModule.moduleUI;
            Atom atom = baseModule.atom;

            searchBox = ui.CreateTextInput("Search For Morph", 800, 100, moduleUI);
            searchBox.transform.localPosition = new Vector3(0, -110, 0);

            paginationSlider = ui.CreateSlider("Page", 930, 80, true, moduleUI);
            paginationSlider.transform.localPosition = new Vector3(0, -200, 0);

            paginationValue = new JSONStorableFloat("Page", 0, (float value) =>
            {

            }, 0, 10, true, true);
            paginationSlider.valueFormat = "n0";
            paginationValue.slider = paginationSlider.slider;

            paginationSlider.gameObject.SetActive(false);

            GridLayoutGroup layout = ui.CreateGridLayout(1200, 800, moduleUI);
            layout.transform.localPosition = new Vector3(0, -1010, 0);
            layout.constraintCount = 3;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.GetComponent<RectTransform>().pivot = new Vector2(0, 0);

            layout.cellSize = new Vector2(400, 80);

            for (int i = 0; i < MAX_MORPHS_PER_PAGE - 1; i++)
            {
                UIDynamicSlider slider = ui.CreateMorphSlider("Slider " + i, 400, 80, moduleUI);
                slider.transform.SetParent(layout.transform, false);

                morphSliders.Add(slider);
                slider.gameObject.SetActive(false);
            }


            DAZCharacterSelector personGeometry = atom.GetStorableByID("geometry") as DAZCharacterSelector;
            morphControl = personGeometry.morphsControlUI;

            regions = new HashSet<string>();
            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                regions.Add(morph.region);
            });

            morphNames = morphControl.GetMorphDisplayNames();

            searchBox.onValueChanged.AddListener(UpdateSearch);

            UIDynamicButton clearButton = ui.CreateButton("Clear", 120, 100, moduleUI);
            clearButton.transform.localPosition = new Vector3(810, -110, 0);
            clearButton.button.onClick.AddListener(() =>
            {
                searchBox.text = "";
                ClearSearch();
            });
            
            UIDynamicButton sortButton = ui.CreateButton("Sorted ByName", 60, 100, moduleUI);
            sortButton.transform.localPosition = new Vector3(1035, -110, 0);
            sortButton.button.onClick.AddListener(() =>
            {
                                                  	switchSort=!switchSort;
                                                  	string storeSearch = searchBox.text;
                                                  	searchBox.text = "";
                                                  	if (switchSort){
                                                  	sortButton.label="Sorted ByValue";
                                                  	} else {
                                                  	sortButton.label="Sorted ByName";
                                                  	}
                                                  	searchBox.text = storeSearch;
            });

            VerticalLayoutGroup commonTermsGroup = ui.CreateVerticalLayout(220, 0, moduleUI);
            commonTermsGroup.transform.localPosition = new Vector3(-230, -200, 0);
            commonTermsGroup.GetComponent<RectTransform>().pivot = new Vector2(0, 0);

            commonTermsGroup.childAlignment = TextAnchor.UpperLeft;


            for (int i = 0; i < MAX_TERMS; i++)
            {
                UIDynamicButton termButton = ui.CreateButton("Term", 220, 40, moduleUI);
                termButton.transform.SetParent(commonTermsGroup.transform, false);
                termButtons.Add(termButton);
                UI.ColorButton(termButton, Color.white, new Color(0.3f, 0.4f, 0.6f));

                termButton.gameObject.SetActive(false);

                ContentSizeFitter csf = termButton.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.MinSize;
            }

            //Debug.Log("----------------------------");
            //UIDynamicSlider testSlider = ui.CreateMorphSlider("test");
            //ui.DebugDeeper(testSlider.transform);
        }

        void ClearSearch()
        {
            termButtons.ForEach((button) =>
            {
                button.gameObject.SetActive(false);
            });

            morphSliders.ForEach((slider) =>
            {
                slider.gameObject.SetActive(false);
            });

            paginationSlider.gameObject.SetActive(false);
        }

        void UpdateSearch(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return;
            }

            List<DAZMorph> foundMorphs = new List<DAZMorph>();

            morphNames.ForEach((name) =>
            {
                bool matches = MatchSearchTerm(name, searchTerm);
                if (matches == false)
                {
                    return;
                }

                foundMorphs.Add(morphControl.GetMorphByDisplayName(name));
            });
            if (switchSort){
            foreach(DAZMorph morph in foundMorphs.OrderByDescending(x => Mathf.Abs(x.appliedValue)))
        	{
            	foundMorphsSorted.Add(morph);
        	}
            foundMorphs = foundMorphsSorted;
            }


            int pages = Mathf.FloorToInt((float)foundMorphs.Count / morphSliders.Count);
            paginationValue.min = 0;
            paginationValue.max = pages;
            paginationValue.val = 0;

            paginationValue.slider.onValueChanged.RemoveAllListeners();
            paginationValue.slider.onValueChanged.AddListener((float newPage) =>
            {
                UpdateSliders(foundMorphs, Mathf.FloorToInt(newPage));
            });

            paginationSlider.gameObject.SetActive(pages > 1);

            UpdateSliders(foundMorphs, 0);

            UpdateCommonTerms(foundMorphs);
        }

        void UpdateSliders(List<DAZMorph> foundMorphs, int page = 0)
        {
            int morphStart = page * morphSliders.Count;

            for (int i = 0; i < morphSliders.Count; i++)
            {
                UIDynamicSlider slider = morphSliders[i];
                slider.slider.onValueChanged.RemoveAllListeners();

                slider.gameObject.SetActive(true);
                if (i >= foundMorphs.Count)
                {
                    slider.gameObject.SetActive(false);
                    continue;
                }

                int morphIndex = morphStart + i;

                if (morphIndex >= foundMorphs.Count)
                {
                    slider.gameObject.SetActive(false);
                    continue;
                }

                DAZMorph morph = foundMorphs[morphIndex];


                slider.label = morph.displayName;// + "(" + morph.region + ")";

                slider.slider.minValue = morph.min;
                slider.slider.maxValue = morph.max;
                slider.slider.value = morph.morphValue;

                slider.slider.onValueChanged.AddListener((float morphValue) =>
                {
                    morph.SetValue(morphValue);
                });
            }
        }

        void UpdateCommonTerms(List<DAZMorph> foundMorphs)
        {
            Dictionary<string, int> wordOccurence = new Dictionary<string, int>();
            foundMorphs.ForEach((DAZMorph morph) =>
            {
                List<string> terms = morph.displayName.Split(' ').ToList();
                terms.ForEach((string term) =>
                {
                    if (wordOccurence.ContainsKey(term) == false)
                    {
                        wordOccurence[term] = 1;
                        return;
                    }
                    else
                    {
                        wordOccurence[term] += 1;
                    }
                });
            });

            List<KeyValuePair<string, int>> sortedTerms = wordOccurence.ToList();

            sortedTerms.Sort(
                (KeyValuePair<string, int> pair1,
                KeyValuePair<string, int> pair2) =>
                {
                    return pair2.Value.CompareTo(pair1.Value);
                }
            );

            for (int i = 0; i < termButtons.Count; i++)
            {
                UIDynamicButton termButton = termButtons[i];
                termButton.button.onClick.RemoveAllListeners();

                if (i >= sortedTerms.Count)
                {
                    termButton.gameObject.SetActive(false);
                    continue;
                }

                termButton.gameObject.SetActive(true);

                KeyValuePair<string, int> term = sortedTerms[i];

                termButton.label = term.Key + " (" + term.Value + ")";
                termButton.button.onClick.AddListener(() =>
                {
                    searchBox.text = searchBox.text + " " + term.Key;
                });
            }
        }

        bool MatchSearchTerm(string morphName, string searchTerm)
        {
            if (searchTerm.Length < 3)
            {
                return false;
            }

            List<string> searchTerms = searchTerm.Split(' ').ToList();

            return searchTerms.TrueForAll((term) =>
            {
                return MatchSingleTerm(morphName, term);
            });
        }

        bool MatchSingleTerm(string morphName, string searchTerm)
        {
            string searchTermLower = searchTerm.ToLower();
            string morphNameLower = morphName.ToLower();
            if (morphNameLower.Contains(searchTermLower))
            {
                return true;
            }

            DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);
            if (morph == null)
            {
                return false;
            }

            string morphRegionLower = morph.region.ToLower();
            if (morphRegionLower.Contains(searchTermLower))
            {
                return true;
            }

            return false;
        }
    }
}
