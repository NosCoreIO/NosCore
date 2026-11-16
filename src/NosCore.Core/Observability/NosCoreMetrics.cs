//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.Metrics;

namespace NosCore.Core.Observability
{
    // documentation/observability.md lists every instrument and how to read them.
    public static class NosCoreMetrics
    {
        public static readonly UpDownCounter<int> SessionsOnline =
            NosCoreTelemetry.Meter.CreateUpDownCounter<int>("noscore.sessions.online", "{session}", "Sessions connected to this server");

        public static readonly Gauge<int> MapPlayers =
            NosCoreTelemetry.Meter.CreateGauge<int>("noscore.map.players", "{player}", "Players on an awake map, by map id");

        public static readonly Gauge<int> MapMonsters =
            NosCoreTelemetry.Meter.CreateGauge<int>("noscore.map.monsters", "{monster}", "Monsters on an awake map, by map id");

        public static readonly Histogram<double> MapTickDuration =
            NosCoreTelemetry.Meter.CreateHistogram<double>("noscore.map.tick.duration", "ms", "Time one life tick of an awake map takes");

        public static readonly Histogram<double> MapExecutorWait =
            NosCoreTelemetry.Meter.CreateHistogram<double>("noscore.map.executor.wait", "ms", "Time work waits for its map's executor");

        public static readonly Counter<long> PacketsHandled =
            NosCoreTelemetry.Meter.CreateCounter<long>("noscore.packets.handled", "{packet}", "Client packets that reached their handler, by header");

        public static readonly Histogram<double> PacketDuration =
            NosCoreTelemetry.Meter.CreateHistogram<double>("noscore.packets.duration", "ms", "Time a packet handler takes, lock wait included, by header");

        public static readonly Counter<long> PacketsRefused =
            NosCoreTelemetry.Meter.CreateCounter<long>("noscore.packets.refused", "{packet}", "Client packets refused before their handler, by reason");

        public static readonly Histogram<double> SaveDuration =
            NosCoreTelemetry.Meter.CreateHistogram<double>("noscore.save.duration", "ms", "Time a character save takes");

        public static readonly Counter<long> SaveFailures =
            NosCoreTelemetry.Meter.CreateCounter<long>("noscore.save.failures", "{save}", "Character saves that were rolled back");

        public static readonly Histogram<double> MasterRequestDuration =
            NosCoreTelemetry.Meter.CreateHistogram<double>("noscore.master.request.duration", "ms", "Round trip of a request to the master, by message type");

        public static readonly Counter<long> MasterRequestTimeouts =
            NosCoreTelemetry.Meter.CreateCounter<long>("noscore.master.request.timeouts", "{request}", "Requests to the master that got no answer in time, by message type");

        public static readonly Counter<long> Failures =
            NosCoreTelemetry.Meter.CreateCounter<long>("noscore.failures", "{failure}", "Exceptions caught at a boundary, by boundary");

        public static readonly Counter<long> PathSearches =
            NosCoreTelemetry.Meter.CreateCounter<long>("noscore.pathfinding.searches", "{search}", "Monster path searches, by whether a path was found");

        public static readonly Histogram<double> PathSearchDuration =
            NosCoreTelemetry.Meter.CreateHistogram<double>("noscore.pathfinding.duration", "ms", "Time one monster path search takes");
    }
}
