namespace BS.CAD.Tools.Core.Abstractions
{
    public interface IPlugin
    {
        string Id { get; }
        string Name { get; }
        void Register(IPluginApi api);
    }
}
