namespace DeluxePlugin.DollMaker
{
    class Module
    {
        protected DollMaker dm;

        protected Atom atom;
        protected Atom person;
        protected UI ui;

        public Module(DollMaker dm)
        {
            this.dm = dm;
            atom = dm.containingAtom;
            person = dm.person;
            ui = dm.ui;
        }

        public virtual void Update()
        {

        }

        public virtual void OnDestroy()
        {

        }
    }
}
