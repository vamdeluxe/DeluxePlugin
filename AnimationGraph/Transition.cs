using UnityEngine;

namespace DeluxePlugin.AnimationGraph
{
    class Transition
    {
        private GameObject lineGO;
        private LineRenderer line;

        public Atom start;
        public Atom end;

        static Vector3 endOffset = new Vector3(0, -0.02f, 0);

        public Transition(Atom start, Atom end)
        {
            this.start = start;
            this.end = end;

            lineGO = new GameObject();

            line = lineGO.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.01f;
            line.endWidth = 0.02f;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.zero);
            line.material = AnimationGraph.lineMaterial;
            line.material.color = new Color(0.85f, 0.89f, 0.98f, 1.0f);
            line.startColor = line.endColor = new Color(1, 1, 1, 1);
        }

        public void Update()
        {
            Vector3 startPos = start.mainController.transform.position;
            startPos -= endOffset;

            line.SetPosition(0, startPos);

            Vector3 endPos = end.mainController.transform.position;
            endPos += endOffset;
            line.SetPosition(1, endPos);

            line.enabled = (SuperController.singleton.editModeToggle.isOn);
        }

        public void OnDestroy()
        {
            GameObject.Destroy(lineGO);
        }
    }
}
