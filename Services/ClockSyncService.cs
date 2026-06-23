using LacunaSpace.Models;

namespace LacunaSpace.Services
{
    public class ProbeClock
    {
        public Probe Probe { get; }
        public long TimeOffset { get; private set; }
        public long RoundTrip { get; private set; }
        public DateTime LastSyncTime { get; private set; }

        public ProbeClock(Probe probe)
        {
            Probe = probe;
            TimeOffset = 0;
            RoundTrip = 0;
            LastSyncTime = DateTime.UtcNow;
        }

        public void AddOffset(long offset)
        {
            TimeOffset += offset;
            LastSyncTime = DateTime.UtcNow;
        }

        public void SetRoundTrip(long roundTrip)
        {
            RoundTrip = roundTrip;
        }

        public long GetSynchronizedTicks()
        {
            var currentTicks = DateTimeOffset.UtcNow.Ticks;
            var elapsedTicks = currentTicks - LastSyncTime.Ticks;

            if (Probe.TimeDilationFactor.HasValue && Probe.TimeDilationFactor.Value > 0)
            {
                elapsedTicks = (long)(elapsedTicks / Probe.TimeDilationFactor.Value);
            }

            return currentTicks + TimeOffset - elapsedTicks;
        }
    }

    public class ClockSyncService
    {
        private readonly ApiClient _apiClient;

        public ClockSyncService(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ProbeClock> SynchronizeWithProbe(Probe probe, CancellationToken cancellationToken = default)
        {
            var clock = new ProbeClock(probe);
            bool retry;

            do
            {
                retry = false;
                try
                {
                    var t0 = DateTimeOffset.UtcNow.Ticks;
                    var syncResponse = await _apiClient.SyncProbeAsync(probe.Id);
                    var t3 = DateTimeOffset.UtcNow.Ticks;

                    if (syncResponse.Code == "ProbeUnreachable")
                    {
                        Console.WriteLine("  ⚠ Probe unreachable. Waiting 5 seconds...");
                        await Task.Delay(5000, cancellationToken);
                        retry = true;
                        continue;
                    }

                    if (syncResponse.Code == "Unauthorized")
                        throw new UnauthorizedAccessException("Access token expired");

                    if (syncResponse.Code != "Success")
                        throw new InvalidOperationException($"Sync failed: {syncResponse.Code} - {syncResponse.Message}");

                    var t1 = TimestampCodec.Decode(syncResponse.T1, probe.Encoding);
                    var t2 = TimestampCodec.Decode(syncResponse.T2, probe.Encoding);

                    var offset = ((t1 - t0) + (t2 - t3)) / 2;
                    var roundTrip = (t3 - t0) - (t2 - t1);

                    clock.AddOffset(offset);
                    clock.SetRoundTrip(roundTrip);

                    var offsetMs = TimeSpan.FromTicks(Math.Abs(offset)).TotalMilliseconds;
                    var roundTripMs = TimeSpan.FromTicks(roundTrip).TotalMilliseconds;

                    Console.WriteLine($"  Offset={offsetMs:F3}ms, RoundTrip={roundTripMs:F3}ms" +
                        (probe.TimeDilationFactor.HasValue ? $", Dilation={probe.TimeDilationFactor.Value}" : ""));

                    Console.WriteLine($"  ✓ Probe {probe.Name} synced (offset={offsetMs:F1}ms)");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"  ⚠ Network error: {ex.Message}. Retrying...");
                    await Task.Delay(1000, cancellationToken);
                    retry = true;
                }
            } while (retry);

            return clock;
        }
    }
}