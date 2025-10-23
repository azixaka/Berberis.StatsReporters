using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace Berberis.StatsReporters;

public sealed class NetworkStatsReporter
{
    public readonly IReadOnlyList<IReadOnlyDictionary<string, string?>> NetworkInfo;

    private long _lastTicks;
    private readonly object _syncObj = new();
    private readonly AdapterStatsReporter[] _adapterStats;

    public NetworkStatsReporter()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        _lastTicks = Stopwatch.GetTimestamp();

        _adapterStats = networkInterfaces.Select(ni => new AdapterStatsReporter(ni)).ToArray();

        NetworkInfo = new List<Dictionary<string, string?>>(networkInterfaces.Select(ni =>
        {
            string? name = null;
            try { name = ni.Name; } catch { }
            string? desc = null;
            try { desc = ni.Description; } catch { }
            string? type = null;
            try { type = ni.NetworkInterfaceType.ToString(); } catch { }
            string? speed = null;
            try { speed = ni.Speed.ToString(); } catch { }
            string? status = null;
            try { status = ni.OperationalStatus.ToString(); } catch { }
            string? multicast = null;
            try { multicast = ni.SupportsMulticast.ToString(); } catch { }

            return new Dictionary<string, string?>
            {
                { "Name", name },
                { "Description", desc },
                { "Type", type },
                { "Speed", speed },
                { "Status", status },
                { "Multicast", multicast }
            };
        }));
    }

    private sealed class AdapterStatsReporter
    {
        private bool _initialized;
        private long _lastRcvBytes;
        private long _lastSndBytes;
        private readonly NetworkInterface _adapter;

        public bool CanBeConsideredPrimary { get; }
        public string AdapterName { get; }

        public AdapterStatsReporter(NetworkInterface adapter)
        {
            _adapter = adapter;

            try
            {
                AdapterName = adapter.Name;
                CanBeConsideredPrimary = adapter.OperationalStatus == OperationalStatus.Up &&
                                         adapter.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                                             or NetworkInterfaceType.Wireless80211;
            }
            catch
            {
                try
                {
                    AdapterName = $"Adapter #{adapter.Id}";
                }
                catch
                {
                    AdapterName = "Unnamed Adapter";
                }
            }
        }

        public InterfaceStats GetStats(float timePassed)
        {
            // the LinuxIPInterfaceStatistics implementation relies on https://github.com/dotnet/corefx/blob/master/src/System.Net.NetworkInformation/src/System/Net/NetworkInformation/StringParsingHelpers.Statistics.cs
            // which artificially limits all values read from Linux network stats table file to UInt64. Couldn't figure out why but it means stats stop after 4bln bytes sent/received.

            var stats = _adapter.GetIPStatistics();
            var rcvd = stats.BytesReceived;
            var sent = stats.BytesSent;

            if (!_initialized)
            {
                _lastRcvBytes = rcvd;
                _lastSndBytes = sent;
                _initialized = true;
                return default;
            }

            var intervalRcvBytes = (rcvd - _lastRcvBytes) * 1000 / timePassed;
            var intervalSndBytes = (sent - _lastSndBytes) * 1000 / timePassed;

            _lastRcvBytes = rcvd;
            _lastSndBytes = sent;

            return new InterfaceStats(_lastRcvBytes, _lastSndBytes, intervalRcvBytes, intervalSndBytes);
        }
    }

    public NetworkStats GetStats()
    {
        lock (_syncObj)
        {
            var ticks = Stopwatch.GetTimestamp();
            var timePassed = (float)(ticks - _lastTicks) / Stopwatch.Frequency * 1000;
            _lastTicks = ticks;

            var statSet = new KeyValuePair<string, InterfaceStats>[_adapterStats.Length];
            var primaryStats = new InterfaceStats();

            for (var i = 0; i < _adapterStats.Length; i++)
            {
                try
                {
                    var adapter = _adapterStats[i];
                    var stats = adapter.GetStats(timePassed);

                    statSet[i] = new KeyValuePair<string, InterfaceStats>(adapter.AdapterName, stats);

                    if (adapter.CanBeConsideredPrimary)
                    {
                        primaryStats = stats;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return new NetworkStats(timePassed, statSet, primaryStats);
        }
    }
}