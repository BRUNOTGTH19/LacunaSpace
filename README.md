# LacunaSpace — Clock Synchronization System

Solução para o desafio técnico de admissão da **Lacuna Software**, implementada em **C# .NET 8**.

O programa se comunica com a API Luma, sincroniza o relógio local com o de cada sonda espacial usando o protocolo NTP simplificado descrito no enunciado, e processa jobs de verificação de relógio até receber a confirmação de conclusão do teste.

---

## Estrutura do projeto

```
LacunaSpace/
├── Models/
│   ├── ApiResponses.cs       # Modelos de request/response da API (DTOs)
│   └── TimestampCodec.cs     # Encode/decode dos 4 formatos de timestamp
├── Services/
│   ├── ApiClient.cs          # Cliente HTTP centralizado (todas as chamadas à API)
│   ├── ClockSyncService.cs   # Protocolo de sincronização de relógio (NTP-like)
│   ├── JobService.cs         # Ciclo de take/check de jobs
│   └── FailReceivedException.cs  # Exceção para sinalizar reinício completo
├── Program.cs                # Ponto de entrada e loop de controle principal
└── LacunaSpace.csproj
```

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Configuração

Antes de rodar, abra `Program.cs` e ajuste as constantes com seus dados:

```csharp
private const string Username = "seu_username";
private const string Email = "seu_email@exemplo.com";
```

---

## Como executar

**Nível 1** (modo padrão):
```bash
dotnet run
```

**Nível 2** (ventos solares + dilatação temporal):
```bash
dotnet run -- 2
```

---

## Como funciona

### 1. Inicialização
Chama `POST /api/start` (ou `/api/start/2`) e obtém um `accessToken` válido por 2 minutos, usado como Bearer token nas demais requisições.

### 2. Listagem de sondas
Chama `GET /api/probe` e obtém a lista de sondas com seus parâmetros: `id`, `name`, `encoding` e, no nível 2, `timeDilationFactor`.

### 3. Sincronização de relógios
Para cada sonda, executa o protocolo de sincronização:

```
t0 = relógio local antes da requisição
  → POST /api/probe/{id}/sync
t3 = relógio local após receber a resposta

t1, t2 = timestamps da sonda (decodificados conforme o encoding da sonda)

θ (offset)     = ((t1 - t0) + (t2 - t3)) / 2
σ (round trip) = (t3 - t0) - (t2 - t1)
```

O offset é acumulado em `ProbeClock.TimeOffset` e o fator de dilatação temporal (quando presente) é aplicado ao tempo decorrido desde a última sincronização ao calcular o timestamp sincronizado.

### 4. Formatos de timestamp suportados (`TimestampCodec`)

| Encoding | Exemplo |
|---|---|
| `Ticks` | `638213938476003807` |
| `Iso8601` | `2023-06-03T12:57:27.6003807+00:00` |
| `TicksBinary` | `37GQFTJk2wg=` (little-endian Base64) |
| `TicksBinaryBigEndian` | `CNtkMhWQsd8=` (big-endian Base64) |

### 5. Processamento de jobs
Em loop: `POST /api/job/take` → sincroniza a sonda do job → `POST /api/job/{id}/check` com o timestamp sincronizado codificado e o round trip. Continua até receber `Done`.

### 6. Tratamento de erros e reinício automático
O programa trata automaticamente dois cenários de reinício completo (novo token + nova lista de sondas + nova sincronização de todas):

- **`Unauthorized`**: token de 2 minutos expirou — reinicia o fluxo do zero.
- **`Fail`**: timestamp entregue fora da tolerância — reinicia o fluxo do zero, conforme exigido pelo enunciado.

No nível 2, respostas `ProbeUnreachable` na sincronização geram uma espera de 5 segundos antes de nova tentativa.

O programa encerra com erro após 10 reinícios consecutivos sem sucesso.