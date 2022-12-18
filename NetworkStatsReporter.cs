using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace Berberis.StatsReporters;

public sealed class NetworkStatsReporter
{
    private long _lastTicks;
    private long _lastRcvBytes;
    private long _lastSndBytes;

    private readonly object _syncObj = new();

    private readonly List<Dictionary<string, string>> _networkInfo;

    private readonly NetworkInterface[] _networkInterfaces;

    public NetworkStatsReporter()
    {
        _networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        if (_networkInterfaces == null)
        {
            _networkInfo = new List<Dictionary<string, string>>(
                                            _networkInterfaces.Select(ni =>
                                            {
                                                string name = null;
                                                try { name = ni.Name; } catch { }

                                                string desc = null;
                                                try { desc = ni.Description; } catch { }

                                                string type = null;
                                                try { type = ni.NetworkInterfaceType.ToString(); } catch { }

                                                string speed = null;
                                                try { speed = ni.Speed.ToString(); } catch { }

                                                string status = null;
                                                try { status = ni.OperationalStatus.ToString(); } catch { }

                                                string multicast = null;
                                                try { multicast = ni.SupportsMulticast.ToString(); } catch { }

                                                return new Dictionary<string, string>
                                                    {
                                                                    {"Name", name},
                                                                    {"Description", desc},
                                                                    {"Type", type},
                                                                    {"Speed", speed},
                                                                    {"Status", status},
                                                                    {"Multicast", multicast}
                                                    };
                                            }));
        }

        _lastTicks = Stopwatch.GetTimestamp();
    }

    public IReadOnlyList<IReadOnlyDictionary<string, string>> NetworkInfo => _networkInfo;

    public NetworkStats GetStats()
    {
        if (_networkInterfaces != null)
        {
            lock (_syncObj)
            {
                var ticks = Stopwatch.GetTimestamp();
                float timePassed = (float)(ticks - _lastTicks) / Stopwatch.Frequency * 1000;
                _lastTicks = ticks;

                var ifaceStats = new (string Name, InterfaceStats Stats)[_networkInterfaces.Length];

                for (var i = 0; i < _networkInterfaces.Length; i++)
                {
                    // the LinuxIPInterfaceStatistics implementation relies on https://github.com/dotnet/corefx/blob/master/src/System.Net.NetworkInformation/src/System/Net/NetworkInformation/StringParsingHelpers.Statistics.cs
                    // which artificially limits all values read from Linux network stats table file to UInt64. Couldn't figure out why but it means stats stop after 4bln bytes sent/received.

                    try
                    {
                        var ni = _networkInterfaces[i];

                        var stats = ni.GetIPStatistics();

                        var rcvd = stats.BytesReceived;
                        var intervalRcvBytes = (rcvd - _lastRcvBytes) * 1000 / timePassed;
                        _lastRcvBytes = rcvd;

                        var sent = stats.BytesSent;
                        var intervalSndBytes = (sent - _lastSndBytes) * 1000 / timePassed;
                        _lastSndBytes = sent;

                        ifaceStats[i] = (ni.Name, new InterfaceStats(_lastRcvBytes, _lastSndBytes, intervalRcvBytes, intervalSndBytes));
                    }
                    catch { }
                }

                return new NetworkStats(timePassed, ifaceStats);
            }
        }

        return default;
    }
}