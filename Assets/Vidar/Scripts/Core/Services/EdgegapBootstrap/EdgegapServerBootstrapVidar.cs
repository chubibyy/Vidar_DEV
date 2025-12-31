using System.Linq;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Edgegap.Bootstrap
{
    public class EdgegapServerBootstrapVidar : EdgegapServerBootstrap
    {
        private (ushort InternalPort, string Protocol, string Host) _sceneTransportData;

        protected override void ValidatePortMapping()
        {
            Debug.Log("[EdgegapBootstrap] Validating Port Mapping for NGO...");
            
            UnityTransport transport = FindFirstObjectByType<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[EdgegapBootstrap] No UnityTransport found in scene!");
                return;
            }

            // By default, Edgegap/Arbitrium tells us what Internal Port we are mapped to,
            // but usually we just bind to 0.0.0.0 and the port we configured in the Dockerfile.
            // However, this script verifies if the environment matches expectations.
            
            _sceneTransportData = (
                transport.ConnectionData.Port,
                "UDP",
                transport.ConnectionData.Address
            );

            // Important: On Edgegap, we must bind to 0.0.0.0 or the specific container IP.
            // If the transport is set to "127.0.0.1", it might fail to receive external traffic.
            if (_sceneTransportData.Host == "127.0.0.1" || _sceneTransportData.Host == "localhost")
            {
                Debug.LogWarning("[EdgegapBootstrap] Transport address is localhost. Changing to 0.0.0.0 for Server.");
                transport.SetConnectionData("0.0.0.0", _sceneTransportData.InternalPort);
            }

            if (_arbitriumPortsMapping == null || _arbitriumPortsMapping.ports == null)
            {
                Debug.LogWarning("[EdgegapBootstrap] No ARBITRIUM_PORTS_MAPPING found. Skipping verification.");
                return;
            }

            // In a real Edgegap deployment, we might want to check which port matches our internal port
            // But usually, we just ensure we listen on the port we defined (e.g. 7777).
            
            Debug.Log($"[EdgegapBootstrap] Bootstrap Complete. Listening on {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
        }
    }
}
