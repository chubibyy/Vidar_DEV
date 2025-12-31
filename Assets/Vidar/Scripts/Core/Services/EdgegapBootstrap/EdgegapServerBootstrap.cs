using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Edgegap.Bootstrap
{
    public abstract class EdgegapServerBootstrap : MonoBehaviour
    {
        public static EdgegapServerBootstrap Instance { get; protected set; }

        protected const string APP_VERSION_PAGE =
            "https://app.edgegap.com/application-management/applications/list";

        protected ArbitriumPortsMapping _arbitriumPortsMapping;
        protected ArbitriumDeploymentLocation _arbitriumDeploymentLocation;
        protected Dictionary<string, string> _arbitriumSimpleEnvs =
            new Dictionary<string, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
            DontDestroyOnLoad(gameObject);
        }

#if !UNITY_CLIENT && !UNITY_EDITOR
        private void Start()
        {
            ParseEdgegapEnvs();
            ValidatePortMapping();
        }
#endif
        // Added for testing in Editor if needed, or manual trigger
        public void ManualStart()
        {
            ParseEdgegapEnvs();
            ValidatePortMapping();
        }

        private void ParseEdgegapEnvs()
        {
            Debug.Log("[EdgegapBootstrap] Parsing Environment Variables...");
            IDictionary envs = Environment.GetEnvironmentVariables();

            foreach (DictionaryEntry envEntry in envs)
            {
                if (!envEntry.Key.ToString().Contains("ARBITRIUM_"))
                {
                    continue;
                }

                string envKey = envEntry.Key.ToString();
                string envValue = envEntry.Value.ToString();

                Debug.Log($"[EdgegapBootstrap] Found Env: {envKey}");

                if (envKey.Contains("DEPLOYMENT_LOCATION"))
                {
                    _arbitriumDeploymentLocation =
                        JsonConvert.DeserializeObject<ArbitriumDeploymentLocation>(envValue);
                }
                else if (envKey.Contains("PORTS_MAPPING"))
                {
                    _arbitriumPortsMapping = JsonConvert.DeserializeObject<ArbitriumPortsMapping>(
                        envValue
                    );
                }
                else
                {
                    _arbitriumSimpleEnvs[envKey] = envValue;
                }
            }
        }

        protected abstract void ValidatePortMapping();
    }

    public class ArbitriumDeploymentLocation
    {
        [JsonProperty("city")]
        public string city { get; private set; }
        [JsonProperty("country")]
        public string country { get; private set; }
        [JsonProperty("continent")]
        public string continent { get; private set; }
        [JsonProperty("administrative_division")]
        public string administrativeDivision { get; private set; }
        [JsonProperty("timezone")]
        public string timezone { get; private set; }
        [JsonProperty("latitude")]
        public float latitude { get; private set; }
        [JsonProperty("longitude")]
        public string longitude { get; private set; }
    }

    public class ArbitriumPortsMapping
    {
        [JsonProperty("ports")]
        public Dictionary<string, PortMappingData> ports { get; private set; }
    }

    public class PortMappingData
    {
        [JsonProperty("name")]
        public string name { get; private set; }
        [JsonProperty("internal")]
        public int internalPort { get; private set; }
        [JsonProperty("external")]
        public int externalPort { get; private set; }
        [JsonProperty("protocol")]
        public string protocol { get; private set; }
    }
}
