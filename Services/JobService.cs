using LacunaSpace.Models;

namespace LacunaSpace.Services
{
    public class JobService
    {
        private readonly ApiClient _apiClient;
        private readonly ClockSyncService _clockSyncService;

        public JobService(ApiClient apiClient, ClockSyncService clockSyncService)
        {
            _apiClient = apiClient;
            _clockSyncService = clockSyncService;
        }

        public async Task ProcessJobs(Dictionary<string, ProbeClock> synchronizedClocks, CancellationToken cancellationToken = default)
        {
            int jobCount = 0;

            while (true)
            {
                var jobResponse = await _apiClient.TakeJobAsync();

                if (jobResponse.Code == "Unauthorized")
                    throw new UnauthorizedAccessException("Access token expired");

                if (jobResponse.Job == null)
                {
                    Console.WriteLine("\nNo more jobs available.");
                    break;
                }

                var job = jobResponse.Job;
                jobCount++;
                Console.WriteLine($"\nJob #{jobCount}: {job.Id} - Probe: {job.ProbeName}");

                if (!synchronizedClocks.TryGetValue(job.ProbeName, out var clock))
                    throw new InvalidOperationException($"Probe {job.ProbeName} not found");

                // Re-sincronizar antes do job
                Console.WriteLine($"  Syncing with {job.ProbeName}...");
                clock = await _clockSyncService.SynchronizeWithProbe(clock.Probe, cancellationToken);
                synchronizedClocks[job.ProbeName] = clock;

                var synchronizedTicks = clock.GetSynchronizedTicks();
                var encodedTimestamp = TimestampCodec.Encode(synchronizedTicks, clock.Probe.Encoding);

                var checkRequest = new JobCheckRequest
                {
                    ProbeNow = encodedTimestamp,
                    RoundTrip = clock.RoundTrip
                };

                var checkResponse = await _apiClient.CheckJobAsync(job.Id, checkRequest);
                Console.WriteLine($"  Response: {checkResponse.Code} - {checkResponse.Message}");

                switch (checkResponse.Code)
                {
                    case "Fail":
                        Console.WriteLine("  ❌ FAIL recebido! Reinício completo necessário.");
                        throw new FailReceivedException();

                    case "Unauthorized":
                        throw new UnauthorizedAccessException("Access token expired");

                    case "Done":
                        Console.WriteLine("\n=========================================");
                        Console.WriteLine("  TEST PASSED! Congratulations!");
                        Console.WriteLine("=========================================");
                        return;

                    case "Success":
                        Console.WriteLine("  ✓ Job completed!");
                        break;
                }
            }

            Console.WriteLine("\nAll jobs processed.");
        }
    }
}