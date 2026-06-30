namespace BS.CAD.Tools.Core.Abstractions
{
    public interface ICommandRegistry
    {
        void Register(string commandName, System.Action execute);
    }
}
