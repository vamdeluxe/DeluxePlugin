using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Powertools
{
    public class Clone : MVRScript
    {

        Atom lastCloned;

        public override void Init()
        {
            try
            {
                CreateButton("Clone Selected (Control+D)").button.onClick.AddListener(CloneSelected);
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        private void CloneSelected()
        {
            Atom selected = SuperController.singleton.GetSelectedAtom();

            if (selected == null)
            {
                return;
            }

            //  Must do this for SuperController.singleton.GetSaveJSON
            SuperController.singleton.currentSaveDir = SuperController.singleton.currentLoadDir;
            StartCoroutine(PerformClone(selected));
        }

        private IEnumerator PerformClone(Atom source)
        {
            SuperController.singleton.PauseSimulation(10, "Cloning" + source.uid);

            string uid = source.name +" Clone " + Guid.NewGuid().ToString();

            yield return StartCoroutine(SuperController.singleton.AddAtomByType(source.type, uid));

            Atom cloned = SuperController.singleton.GetAtomByUid(uid);

            if (lastCloned == null)
            {
                lastCloned = source;
            }

            Transform transform = cloned.GetComponent<Transform>();
            transform.position = lastCloned.mainController.transform.position;
            transform.Translate(1, 0, 1);


            JSONClass sourceSceneJSON = SuperController.singleton.GetSaveJSON(source, true, true);
            JSONNode sourceJSON = sourceSceneJSON["atoms"].AsArray[0];

            //Debug.Log(sourceJSON);
            //Debug.Log("cloning properties");

            cloned.PreRestore();
            cloned.Restore(sourceJSON as JSONClass, false, true, true);
            cloned.LateRestore(sourceJSON as JSONClass, false, true, true);
            cloned.PostRestore();

            lastCloned = cloned;

            SuperController.singleton.SelectController(lastCloned.mainController);
        }

        void Update()
        {
            try
            {
                if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        CloneSelected();
                    }
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

    }
}
