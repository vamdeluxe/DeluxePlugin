using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.AnimationGraph
{
    public class AnimationGraph : MVRScript
    {
        UI ui;
        Node root;
        Node active;
        List<Node> nodes = new List<Node>();
        List<Transition> transitions = new List<Transition>();
        SuperController sc;

        JSONStorableString saveString;

        JSONStorableAction playNextWhenReady;

        public static Material lineMaterial;

        bool started = false;

        public override void Init()
        {
            try
            {
                sc = SuperController.singleton;

                ui = new UI(this, 0.001f);
                ui.canvas.transform.SetParent(containingAtom.mainController.transform, false);

                UIDynamicButton entrySelector = ui.CreateButton("Select Entry");
                entrySelector.transform.Translate(0, 0.1f, 0);
                entrySelector.button.onClick.AddListener(() =>
                {
                    sc.SelectModeAtom((atom) =>
                    {
                        AnimationPattern ap = atom.GetStorableByID("AnimationPattern") as AnimationPattern;
                        if (ap == null)
                        {
                            SuperController.LogError("Only Animation Patterns can be selected.");
                            return;
                        }

                        root = CreateNode(ap);
                        CreateTransition(containingAtom, root.atom);

                        active = root;

                        entrySelector.gameObject.SetActive(false);
                        PerformSave();
                    });
                });

                UIDynamicButton playButton = ui.CreateButton("Test Start", 80, 60);
                playButton.transform.Translate(0, -0.2f, 0);
                playButton.button.onClick.AddListener(() =>
                {
                    if (root == null)
                    {
                        return;
                    }

                    active = root;
                    active.Start();
                });

                UIDynamicButton nextButton = ui.CreateButton("Test Next", 80, 60);
                nextButton.transform.Translate(0, -0.3f, 0);
                nextButton.button.onClick.AddListener(() =>
                {
                    if (active == null)
                    {
                        return;
                    }

                    Transition transition = active.GetRandomTransition();
                    if (transition != null)
                    {
                        Node nextNode = GetEndNodeFromTransition(transition);
                        if (nextNode != null)
                        {
                            active.Stop();
                            nextNode.Start();
                            active = nextNode;
                        }
                    }
                });

                UIDynamicButton doneEditingButton = ui.CreateButton("Done Editing", 80, 60);
                doneEditingButton.transform.Translate(0, -0.6f, 0);
                doneEditingButton.button.onClick.AddListener(() =>
                {
                    ui.canvas.enabled = false;
                    if (sc.GetSelectedAtom() == containingAtom)
                    {
                        sc.ClearSelection();
                    }
                });

                saveString = new JSONStorableString("graph", "");
                RegisterString(saveString);

                playNextWhenReady = new JSONStorableAction("play next if ready", () =>
                {
                    if (active == null)
                    {
                        return;
                    }

                    if (started == false)
                    {
                        active.Start();
                        started = true;
                        return;
                    }

                    if (active.ap.loop == false)
                    {
                        if (active.ap.GetCurrentTimeCounter() < active.ap.GetTotalTime())
                        {
                            return;
                        }
                    }

                    Transition transition = active.GetRandomTransition();
                    if (transition != null)
                    {
                        Node nextNode = GetEndNodeFromTransition(transition);
                        if (nextNode != null)
                        {
                            active.Stop();
                            nextNode.Start();
                            active = nextNode;
                        }
                    }
                });

                RegisterAction(playNextWhenReady);
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Start()
        {
            if (saveString.val != saveString.defaultVal)
            {
                JSONClass loadData = JSON.Parse(saveString.val) as JSONClass;
                Debug.Log(loadData);
                StartCoroutine(Load(loadData));
            }
        }

        void Update()
        {
            ui.Update();

            if(sc.GetSelectedAtom() == containingAtom && SuperController.singleton.editModeToggle.isOn)
            {
                ui.canvas.enabled = true;
            }

            nodes.ForEach((node) =>
            {
                node.Update();
            });

            transitions.ForEach((transition) =>
            {
                transition.Update();
            });
        }

        void OnDestroy()
        {
            if (ui != null)
            {
                ui.OnDestroy();
            }
            transitions.ForEach((node) =>
            {
                node.OnDestroy();
            });
        }

        #region Implementation

        IEnumerator Load(JSONClass saveObject)
        {
            yield return new WaitForSeconds(2.0f);
            JSONArray savedNodes = saveObject["nodes"].AsArray;
            JSONArray savedTransitions = saveObject["transitions"].AsArray;

            for(int i = 0; i < savedNodes.Count; i++)
            {
                Atom a = sc.GetAtomByUid(savedNodes[i]["atom"].Value);
                if (a == null)
                {
                    SuperController.LogError("Animation graph could not find the atom named " + savedNodes[i]["atom"].Value);
                    continue;
                }

                AnimationPattern ap = a.GetStorableByID("AnimationPattern") as AnimationPattern;
                if (ap == null)
                {
                    SuperController.LogError(a.name + " is not an animation pattern");
                    continue;
                }

                CreateNode(ap);
            }

            if (nodes.Count > 0)
            {
                root = nodes[0];
            }

            for(int i=0; i<savedTransitions.Count; i++)
            {
                JSONClass transitionData = savedTransitions[i].AsObject;
                string startName = transitionData["start"].Value;
                string endName = transitionData["end"].Value;

                Atom a = sc.GetAtomByUid(startName);
                Atom b = sc.GetAtomByUid(endName);

                if (a==null)
                {
                    SuperController.LogError(startName + " atom was not found");
                    continue;
                }

                if(b==null)
                {
                    SuperController.LogError(endName + " atom was not found");
                    continue;
                }


                Transition t = CreateTransition(a,b);

                if (t != null)
                {
                    Node nodeA = FindNodeFromAtom(a);
                    if (nodeA != null)
                    {
                        nodeA.transitions.Add(t);
                    }
                }
            }
        }

        JSONClass ToJSON()
        {
            JSONClass save = new JSONClass();
            JSONArray savedNodes = new JSONArray();
            save["nodes"] = savedNodes;

            nodes.ForEach((node) =>
            {
                JSONClass nodeObject = new JSONClass();
                nodeObject["atom"] = node.ap.containingAtom.name;
                    //Debug.Log(node.ap.containingAtom.name);
                    savedNodes.Add(nodeObject);
            });

            JSONArray savedTransitions = new JSONArray();
            save["transitions"] = savedTransitions;

            transitions.ForEach((t) =>
            {
                JSONClass transitionObject = new JSONClass();
                transitionObject["start"] = t.start.name;
                transitionObject["end"] = t.end.name;
                savedTransitions.Add(transitionObject);
            });

            return save;
        }

        void PerformSave()
        {
            saveString.SetVal(ToJSON().ToString());
        }

        Node CreateNode(AnimationPattern ap)
        {

            Node foundNode = nodes.Find((other) =>
            {
                return other.ap == ap;
            });

            if (foundNode != null)
            {
                sc.SelectController(containingAtom.mainController);
                return foundNode;
            }

            Node node = new Node(ui, ap);

            //ap.containingAtom.SetOn(false);
            ap.SetBoolParamValue("on", false);
            ap.Pause();
            ap.ResetAnimation();

            lineMaterial = ap.rootLineDrawerMaterial;

            if (nodes.Contains(node) == false)
            {
                nodes.Add(node);
            }

            node.connectButton.button.onClick.AddListener(() =>
            {
                sc.SelectModeAtom((atom) =>
                {
                    if (atom == ap.containingAtom)
                    {
                        SuperController.LogError("Can't make a connection to itself.");
                        return;
                    }
                    AnimationPattern childAP = atom.GetStorableByID("AnimationPattern") as AnimationPattern;
                    if (childAP == null)
                    {
                        SuperController.LogError("Only Animation Patterns can be selected.");
                        return;
                    }

                    Node childNode = CreateNode(childAP);
                    Transition transition = CreateTransition(node.atom, childNode.atom);
                    if (transition != null)
                    {
                        node.transitions.Add(transition);
                        PerformSave();
                    }
                });
            });

            node.disconnectButton.button.onClick.AddListener(() =>
            {
                node.transitions.ForEach((t) =>
                {
                    t.OnDestroy();
                    transitions.Remove(t);
                });

                node.transitions.Clear();
            });

            sc.SelectController(containingAtom.mainController);

            return node;
        }

        Transition CreateTransition(Atom start, Atom end)
        {
            Transition foundT = transitions.Find((other) =>
            {
                return other.start == start && other.end == end;
            });
            if (foundT != null)
            {
                SuperController.LogMessage("connection already exists");
                return null;
            }

            Transition t = new Transition(start, end);
            transitions.Add(t);
            return t;
        }

        Node GetEndNodeFromTransition(Transition t)
        {
            Atom a = t.end;
            if (a == null)
            {
                return null;
            }

            return FindNodeFromAtom(a);
        }

        Node FindNodeFromAtom(Atom atom)
        {
            return nodes.Find((node) =>
            {
                return node.ap.containingAtom == atom;
            });
        }

        #endregion
    }
}
