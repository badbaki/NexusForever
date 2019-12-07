using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace NexusForever.Shared.Configuration
{
    public sealed class ConfigurationManager<T> : Singleton<ConfigurationManager<T>>
    {
        public T Config { get; private set; }
        public IConfiguration GetConfiguration() => SharedConfiguration.Configuration;

        private ConfigurationManager()
        {
        }

        public void Initialise(string file)
        {
            SharedConfiguration.Initialise(file);
            Config = SharedConfiguration.Configuration.Get<T>();
        }

        public void Save()
        {
            SharedConfiguration.Save<T>(Config);
        }
    }
}
