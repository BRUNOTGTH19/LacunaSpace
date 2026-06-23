using LacunaSpace.Models;
using LacunaSpace.Services;

namespace LacunaSpace
{
    class Program
    {
        
        private const string Username = "seu_usuario"; //Subistitua pelo seu nome de usuário
        private const string Email = "seu_email"; //Subistitua pelo seu email

        private const int MaxRestarts = 10;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // dotnet run -- 2  -> roda no level 2
            int level = args.Length > 0 && int.TryParse(args[0], out var l) ? l : 1;

            using var api = new ApiClient();
            var sync = new ClockSyncService(api);
            var jobs = new JobService(api, sync);
            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            int restart = 0;

            while (true)
            {
                try
                {
                    await RunTestAsync(api, sync, jobs, level, ct);
                    break; // TEST PASSED
                }
                catch (UnauthorizedAccessException)
                {
                    if (++restart > MaxRestarts)
                        throw new Exception("Número máximo de reinícios excedido (token expirado).");

                    Console.WriteLine("\n❌ Token expirado. Reinício completo necessário.");
                    Console.WriteLine($"\n🔄 Reinício completo #{restart} (novo token, nova sincronização)...\n");
                }
                catch (FailReceivedException)
                {
                    if (++restart > MaxRestarts)
                        throw new Exception("Número máximo de reinícios excedido (resposta 'Fail').");

                    Console.WriteLine($"\n🔄 Reinício completo #{restart} (novo token, nova sincronização)...\n");
                }
            }
        }

        private static async Task RunTestAsync(ApiClient api, ClockSyncService sync, JobService jobs, int level, CancellationToken ct)
        {
            // 1. Start
            Console.WriteLine("1️⃣  Starting...");
            var start = await api.StartAsync(Username, Email, level);

            if (start.Code != "Success")
                throw new Exception($"Start failed: {start.Message}");

            api.SetAuthorization(start.AccessToken!);
            Console.WriteLine("✓ Started!\n");

            // 2. Listar sondas
            Console.WriteLine("2️⃣  Getting probes...");
            var probesResp = await api.GetProbesAsync();

            if (probesResp.Code != "Success" || probesResp.Probes == null)
                throw new Exception($"Probes failed: {probesResp.Message}");

            Console.WriteLine($"✓ {probesResp.Probes.Count} probes found:");
            foreach (var probe in probesResp.Probes)
            {
                Console.WriteLine($"  • {probe.Name} ({probe.Encoding}" +
                    (probe.TimeDilationFactor.HasValue ? $", Dilation={probe.TimeDilationFactor.Value:F2}x" : "") + ")");
            }
            Console.WriteLine();

            // 3. Sincronizar relógios
            Console.WriteLine("3️⃣  Synchronizing...");
            var clocks = new Dictionary<string, ProbeClock>();

            foreach (var probe in probesResp.Probes)
            {
                Console.WriteLine($"\nProbe: {probe.Name}");
                clocks[probe.Name] = await sync.SynchronizeWithProbe(probe, ct);
            }

            Console.WriteLine("\n✓ All synchronized!\n");

            // 4. Processar jobs
            Console.WriteLine("4️⃣  Processing jobs...");
            await jobs.ProcessJobs(clocks, ct);
        }
    }
}