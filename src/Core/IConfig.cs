namespace QoLChanges.Core
{
    public interface IConfig
    {
        /// <summary>Clamps or repairs values after the config has been read from disk.</summary>
        void Normalize();
    }
}
