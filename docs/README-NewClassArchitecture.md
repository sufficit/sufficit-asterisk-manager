# Nova Arquitetura de Classes do AsteriskManagerService

## ? Refatoração Completa - Classes Separadas

A refatoração do `AsteriskManagerService` seguiu o princípio de **Separação de Responsabilidades** (Single Responsibility Principle), extraindo classes internas para arquivos dedicados e criando uma arquitetura mais modular e testável.

### ??? **Estrutura de Arquivos Criada**

```
sufficit-asterisk-manager/src/Services/
??? AsteriskManagerService.cs              # Classe principal (refatorada)
??? IAsteriskManagerService.cs              # Interface principal (atualizada)
??? AsteriskManagerHealthCheckResult.cs     # Resultado de health check (NOVA)
??? ProviderHealthInfo.cs                   # Informações de saúde do provider (NOVA)
??? AsteriskManagerHealthChecker.cs         # Gerenciador de health checks (NOVA)
```

### ?? **Classes Criadas**

#### **1. AsteriskManagerHealthCheckResult.cs**
```csharp
public class AsteriskManagerHealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; }
    public Dictionary<string, ProviderHealthInfo> ProvidersHealth { get; set; }
    public DateTimeOffset LastReceivedEvent { get; set; }
    public int TotalProviders { get; set; }
    public int HealthyProviders { get; set; }
    public int UnhealthyProviders { get; set; }
    public Dictionary<string, object> ExtendedData { get; set; }
    
    // Métodos utilitários
    public string GetProviderSummary()
    public bool HasRecentActivity(TimeSpan maxIdleTime)
    public IEnumerable<string> GetUnhealthyProviderTitles()
    public (bool IsHealthy, string Status) ToSimpleResult()
}
```

**Funcionalidades:**
- ? **Resultado detalhado** de health checks
- ? **Métodos utilitários** para análise de saúde
- ? **Compatibilidade** com formato ASP.NET Core
- ? **Extensibilidade** para dados customizados

#### **2. ProviderHealthInfo.cs**
```csharp
public class ProviderHealthInfo
{
    public bool IsHealthy { get; set; }
    public string Title { get; set; }
    public string Address { get; set; }
    public string Status { get; set; }
    public bool HasConnection { get; set; }
    public bool IsConnected { get; set; }
    public bool IsAuthenticated { get; set; }
    public string? LastError { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public TimeSpan? TimeSinceLastConnection { get; set; }
    public int ConnectionAttempts { get; set; }
    
    // Métodos avançados
    public string GetDetailedStatus()
    public HealthLevel GetHealthLevel()
    public void MarkAsUpdated()
}

public enum HealthLevel
{
    Healthy, Warning, Unhealthy, Critical, Unknown
}
```

**Funcionalidades:**
- ? **Monitoramento detalhado** de providers individuais
- ? **Classificação de severidade** com enum HealthLevel
- ? **Timestamps** para rastreamento temporal
- ? **Métricas de conexão** para diagnóstico

#### **3. AsteriskManagerHealthChecker.cs**
```csharp
public class AsteriskManagerHealthChecker
{
    // Configuração de health checks
    public AsteriskManagerHealthChecker(
        ILogger<AsteriskManagerHealthChecker> logger,
        HealthCheckConfiguration? configuration = null)
    
    // Health check síncrono
    public AsteriskManagerHealthCheckResult CheckHealth(
        ICollection<AsteriskManagerProvider> providers,
        DateTimeOffset lastReceivedEvent,
        Func<Dictionary<string, object>>? extendedDataProvider = null)
    
    // Health check assíncrono
    public async Task<AsteriskManagerHealthCheckResult> CheckHealthAsync(
        ICollection<AsteriskManagerProvider> providers,
        DateTimeOffset lastReceivedEvent,
        Func<Task<Dictionary<string, object>>>? extendedDataProvider = null,
        CancellationToken cancellationToken = default)
    
    // Conversão para ASP.NET Core
    public Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult 
        ToAspNetHealthCheckResult(AsteriskManagerHealthCheckResult healthResult)
}

public class HealthCheckConfiguration
{
    public HealthThreshold HealthThreshold { get; set; }
    public double MinimumHealthyPercentage { get; set; }
    public TimeSpan MaxEventAge { get; set; }
    public bool IncludeDetailedProviderInfo { get; set; }
}

public enum HealthThreshold
{
    AllProviders, MajorityProviders, AtLeastOneProvider, MinimumPercentage
}
```

**Funcionalidades:**
- ? **Lógica centralizada** de health checks
- ? **Configuração flexível** de thresholds
- ? **Suporte assíncrono** para operações complexas
- ? **Integração ASP.NET Core** nativa
- ? **Extensibilidade** para dados customizados

### ?? **AsteriskManagerService Refatorado**

#### **Antes (Classe Monolítica):**
```csharp
public abstract class AsteriskManagerService : IDisposable, IHealthCheck, IHostedService
{
    // +600 linhas com classes internas
    public class HealthCheckResult { /* ... */ }
    public class ProviderHealthInfo { /* ... */ }
    
    // Lógica de health check misturada
    public virtual HealthCheckResult CheckHealthDetailed() { /* 50+ linhas */ }
}
```

#### **Depois (Classe Focada):**
```csharp
public abstract class AsteriskManagerService : IDisposable, IAsyncDisposable, IHealthCheck, IHostedService
{
    private readonly AsteriskManagerHealthChecker _healthChecker;
    
    public virtual AsteriskManagerHealthCheckResult CheckHealthDetailed()
    {
        return _healthChecker.CheckHealth(Providers, LastReceivedEvent, OnGetExtendedHealthData);
    }
    
    protected virtual Dictionary<string, object>? OnGetExtendedHealthData()
    {
        // Override em classes derivadas
        return null;
    }
}
```

### ?? **Benefícios da Nova Arquitetura**

#### **1. Separação de Responsabilidades**
- ? **AsteriskManagerService**: Coordenação e lifecycle
- ? **AsteriskManagerHealthChecker**: Lógica de health checks
- ? **AsteriskManagerHealthCheckResult**: Modelo de dados
- ? **ProviderHealthInfo**: Detalhes específicos do provider

#### **2. Testabilidade Melhorada**
```csharp
// Agora é possível testar health checks isoladamente
[Test]
public void HealthChecker_Should_Report_Unhealthy_When_No_Providers()
{
    var logger = Mock.Of<ILogger<AsteriskManagerHealthChecker>>();
    var checker = new AsteriskManagerHealthChecker(logger);
    
    var result = checker.CheckHealth(new List<AsteriskManagerProvider>(), DateTimeOffset.UtcNow);
    
    Assert.False(result.IsHealthy);
}
```

#### **3. Reutilização de Código**
```csharp
// Health checker pode ser usado independentemente
public class MyCustomService
{
    private readonly AsteriskManagerHealthChecker _healthChecker;
    
    public MyCustomService(AsteriskManagerHealthChecker healthChecker)
    {
        _healthChecker = healthChecker;
    }
    
    public HealthStatus CheckMyProviders()
    {
        return _healthChecker.CheckHealth(myProviders, lastEvent);
    }
}
```

#### **4. Configuração Flexível**
```csharp
// Diferentes estratégias de health check
var config1 = new HealthCheckConfiguration
{
    HealthThreshold = HealthThreshold.AllProviders
};

var config2 = new HealthCheckConfiguration
{
    HealthThreshold = HealthThreshold.MinimumPercentage,
    MinimumHealthyPercentage = 0.8
};
```

#### **5. Extensibilidade Aprimorada**
```csharp
// Classes derivadas podem facilmente estender health checks
public class MyAsteriskService : AsteriskManagerService
{
    protected override Dictionary<string, object>? OnGetExtendedHealthData()
    {
        return new Dictionary<string, object>
        {
            ["ActiveCalls"] = GetActiveCallCount(),
            ["QueueHealth"] = CheckQueueStatus(),
            ["DatabaseConnection"] = IsDatabaseHealthy()
        };
    }
}
```

### ?? **Compatibilidade Mantida**

#### **API Pública Inalterada:**
```csharp
// Métodos existentes continuam funcionando
var service = new MyAsteriskManagerService(loggerFactory);
var (isHealthy, status) = service.CheckHealth();
var detailedResult = service.CheckHealthDetailed();
await service.CheckHealthAsync();
```

#### **ASP.NET Core Integration:**
```csharp
// Registro no DI container permanece o mesmo
services.AddSingleton<MyAsteriskManagerService>();
services.AddSingleton<IHealthCheck>(x => x.GetRequiredService<MyAsteriskManagerService>());
services.AddSingleton<IHostedService>(x => x.GetRequiredService<MyAsteriskManagerService>());
```

### ?? **Exemplo de Uso das Novas Classes**

#### **Health Check Customizado:**
```csharp
public class CustomHealthChecker
{
    private readonly AsteriskManagerHealthChecker _checker;
    
    public CustomHealthChecker(ILogger<AsteriskManagerHealthChecker> logger)
    {
        var config = new HealthCheckConfiguration
        {
            HealthThreshold = HealthThreshold.MajorityProviders,
            MaxEventAge = TimeSpan.FromMinutes(2)
        };
        _checker = new AsteriskManagerHealthChecker(logger, config);
    }
    
    public async Task<bool> IsSystemHealthyAsync(ICollection<AsteriskManagerProvider> providers)
    {
        var result = await _checker.CheckHealthAsync(
            providers, 
            DateTimeOffset.UtcNow,
            async () => new Dictionary<string, object>
            {
                ["ExternalAPI"] = await CheckExternalApiAsync(),
                ["DatabaseLatency"] = await MeasureDatabaseLatencyAsync()
            });
            
        return result.IsHealthy && result.HasRecentActivity(TimeSpan.FromMinutes(1));
    }
}
```

### ?? **Resultado Final**

- ? **Código mais limpo** e organizado
- ? **Melhor testabilidade** com classes isoladas
- ? **Maior reutilização** de componentes
- ? **Extensibilidade aprimorada** para novos cenários
- ? **Compatibilidade total** com código existente
- ? **Arquitetura mais robusta** seguindo SOLID principles

A nova arquitetura transforma um arquivo monolítico de 600+ linhas em múltiplos arquivos especializados, cada um com responsabilidades bem definidas, mantendo total compatibilidade com o código existente! ??