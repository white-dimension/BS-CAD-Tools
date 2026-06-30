namespace BS.CAD.Tools.Core.Abstractions
{
    public interface IPluginApi
    {
        ILayerEngine Layers { get; }
        IAuditEngine Audit { get; }
        ICommandRegistry Commands { get; }
    }
}
