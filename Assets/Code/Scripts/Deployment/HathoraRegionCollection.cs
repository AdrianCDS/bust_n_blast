using HathoraCloud.Models.Shared;
using System.Collections.Generic;
using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using HathoraCloud;
using HathoraCloud.Models.Operations;

namespace TeamBasedShooter
{
    public static class HathoraRegionCollection
    {
        private static readonly Dictionary<Region, string> _hathoraToPhoton = new Dictionary<Region, string>()
        {
            { Region.Seattle, "usw"},
            { Region.LosAngeles, "usw" },
            { Region.Dallas, "usw" },
            { Region.WashingtonDC, "us" },
            { Region.Chicago, "us" },
            { Region.London,  "eu" },
            { Region.Frankfurt, "eu" },
            { Region.Mumbai, "in" },
            { Region.Singapore, "asia" },
            { Region.Tokyo, "jp" },
            { Region.Sydney, "jp" },
            { Region.SaoPaulo,"sa" }
        };

        private static readonly Dictionary<string, Region> _photonToHathora = new Dictionary<string, Region>()
        {
            { "us", Region.WashingtonDC },
            { "usw", Region.Dallas },
            { "asia", Region.Singapore },
            { "jp", Region.Tokyo },
            { "eu", Region.Frankfurt },
            { "sa", Region.SaoPaulo },
            { "in", Region.Mumbai },
            { "kr", Region.Tokyo }
        };

        public static string HathoraToPhoton(Region hathoraRegion)
        {
            return _hathoraToPhoton[hathoraRegion];
        }

        public static Region PhotonToHathora(string photonRegion)
        {
            return _photonToHathora[photonRegion];
        }

        public static async Task<(bool bestRegionFound, Region bestRegion, double bestRegionPing)> FindBestRegion(HathoraCloudSDK hathoraCloudSDK, Region fallbackRegion, bool enableLogs = false)
        {
            // 1. Get all Hathora endpoints.
            GetPingServiceEndpointsResponse pingEndpointsResponse = await hathoraCloudSDK.DiscoveryV2.GetPingServiceEndpointsAsync();
            if (pingEndpointsResponse.PingEndpoints == null)
                return (false, fallbackRegion, default);

            // 2. Create ping request.
            List<Tuple<Region, List<Ping>>> regionPings = new List<Tuple<Region, List<Ping>>>();
            foreach (PingEndpoints endpoint in pingEndpointsResponse.PingEndpoints)
            {
                string ip = TryGetIPAddress(endpoint.Host);

                // 6 pings for each endpoint, then calculating average ping.
                List<Ping> pings = new()
                {
                    new Ping(ip),
                    new Ping(ip),
                    new Ping(ip),
                    new Ping(ip),
                    new Ping(ip),
                    new Ping(ip)
                };
                regionPings.Add(new Tuple<Region, List<Ping>>(endpoint.Region, pings));
            }

            // 3. Wait for ping responses.
            const int delay = 20;
            const int iterations = 50;
            for (int i = 0; i < iterations; ++i)
            {
                await Task.Delay(delay);

                bool allPingsDone = true;

                foreach (Tuple<Region, List<Ping>> regionPing in regionPings)
                {
                    foreach (Ping ping in regionPing.Item2)
                    {
                        allPingsDone &= ping.isDone;
                    }
                }

                if (allPingsDone == true)
                {
                    break;
                }
            }

            // 4. Find best region with lowest ping response.
            Region bestRegion = fallbackRegion;
            double bestRegionPing = Double.MaxValue;
            bool bestRegionFound = false;

            foreach (Tuple<Region, List<Ping>> regionPing in regionPings)
            {
                double pingTime = 0.0;
                int pingCount = 0;

                foreach (Ping ping in regionPing.Item2)
                {
                    if (ping.isDone == true && ping.time > 0)
                    {
                        if (enableLogs == true)
                        {
                            Debug.Log($"Region: {regionPing.Item1}   IP: {ping.ip}   Ping: {ping.time}ms");
                        }

                        pingCount++;
                        pingTime += ping.time;
                    }
                    else
                    {
                        if (enableLogs == true)
                        {
                            Debug.LogWarning($"Region: {regionPing.Item1}   IP: {ping.ip}   Ping: ---ms");
                        }
                    }
                }

                if (pingCount > 0)
                {
                    double averageRegionPing = pingTime / pingCount;
                    if (averageRegionPing < bestRegionPing)
                    {
                        bestRegion = regionPing.Item1;
                        bestRegionPing = averageRegionPing;
                        bestRegionFound = true;
                    }
                }
            }

            return (bestRegionFound, bestRegion, bestRegionPing);
        }

        private static string TryGetIPAddress(string hostname)
        {
            IPHostEntry host = Dns.GetHostEntry(hostname);

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }

            return hostname;
        }
    }
}
