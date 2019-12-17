namespace DeluxePlugin.Qualia
{
    interface IQualiaModule
    {
        void Init(SharedBehaviors plugin);
        void Update(SharedBehaviors plugin);
        void Destroy(SharedBehaviors plugin);
    }
}