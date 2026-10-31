using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NuclearProtection.Network
{
    /// <summary>Liste les adresses IPv4 locales à communiquer aux amis (LAN, Tailscale, ZeroTier…).</summary>
    public static class LocalIpUtility
    {
        public static List<string> GetLocalIPv4Addresses()
        {
            var result = new List<string>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // On ignore les interfaces coupées et la boucle locale.
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (IPAddress.IsLoopback(addr.Address)) continue;
                    result.Add($"{addr.Address}  ({nic.Name})");
                }
            }
            if (result.Count == 0) result.Add("127.0.0.1  (aucune interface réseau)");
            return result;
        }

        /// <summary>Vérifie qu'une saisie est bien une adresse IPv4.</summary>
        public static bool IsValidIPv4(string text) =>
            IPAddress.TryParse(text?.Trim(), out var ip) && ip.AddressFamily == AddressFamily.InterNetwork;
    }
}
