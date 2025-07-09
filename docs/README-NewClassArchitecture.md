# Nova Arquitetura de Classes do AsteriskManagerService

## ? Refatora��o Completa - Classes Separadas

A refatora��o do `AsteriskManagerService` seguiu o princ�pio de **Separa��o de Responsabilidades** (Single Responsibility Principle), extraindo classes internas para arquivos dedicados e criando uma arquitetura mais modular e test�vel.

### ??? **Estrutura de Arquivos Criada**

```
sufficit-asterisk-manager/src/Services/
??? AsteriskManagerService.cs              # Classe principal (refatorada)
??? IAsteriskManagerService.cs              # Interface principal (atualizada)
??? AsteriskManagerHealthCheckResult.cs     # Resultado de health check (NOVA)
??? ProviderHealthInfo.cs                   # Informa��es de sa�de do provider (NOVA)
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
    
    // M�todos utilit�rios
    public string GetProviderSummary()
    public bool HasRecentActivity(TimeSpan maxIdleTime)
    public IEnumerable<string> GetUnhealthyProviderTitles()
    public (bool IsHealthy, string Status) ToSimpleResult()
}
```

**Funcionalidades:**
- ? **Resultado detalhado** de health checks
- ? **M�todos utilit�rios** para an�lise de sa�de
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
    
    // M�todos avan�ados
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
- ? **Classifica��o de severidade** com enum HealthLevel
- ? **Timestamps** para rastreamento temporal
- ? **M�tricas de conex�o** para diagn�stico

#### **3. AsteriskManagerHealthChecker.cs**
```csharp
public class AsteriskManagerHealthChecker
{
    // Configura��o de health checks
    public AsteriskManagerHealthChecker(
        ILogger<AsteriskManagerHealthChecker> logger,
        HealthCheckConfiguration? configuration = null)
    
    // Health check s�ncrono
    public AsteriskManagerHealthCheckResult CheckHealth(
        ICollection<AsteriskManagerProvider> providers,
        DateTimeOffset lastReceivedEvent,
        Func<Dictionary<string, object>>? extendedDataProvider = null)
    
    // Health check ass�ncrono
    public async Task<AsteriskManagerHealthCheckResult> CheckHealthAsync(
        ICollection<AsteriskManagerProvider> providers,
        DateTimeOffset lastReceivedEvent,
        Func<Task<Dictionary<string, object>>>? extendedDataProvider = null,
        CancellationToken cancellationToken = default)
    
    // Convers�o para ASP.NET Core
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
- ? **L�gica centralizada** de health checks
- ? **Configura��o flex�vel** de thresholds
- ? **Suporte ass�ncrono** para opera��es complexas
- ? **Integra��o ASP.NET Core** nativa
- ? **Extensibilidade** para dados customizados

### ?? **AsteriskManagerService Refatorado**

#### **Antes (Classe Monol�tica):**
```csharp
public abstract class AsteriskManagerService : IDisposable, IHealthCheck, IHostedService
{
    // +600 linhas com classes internas
    public class HealthCheckResult { /* ... */ }
    public class ProviderHealthInfo { /* ... */ }
    
    // L�gica de health check misturada
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

### ?? **Benef�cios da Nova Arquitetura**

#### **1. Separa��o de Responsabilidades**
- ? **AsteriskManagerService**: Coordena��o e lifecycle
- ? **AsteriskManagerHealthChecker**: L�gica de health checks
- ? **AsteriskManagerHealthCheckResult**: Modelo de dados
- ? **ProviderHealthInfo**: Detalhes espec�ficos do provider

#### **2. Testabilidade Melhorada**
```csharp
// Agora � poss�vel testar health checks isoladamente
[Test]
public void HealthChecker_Should_Report_Unhealthy_When_No_Providers()
{
    var logger = Mock.Of<ILogger<AsteriskManagerHealthChecker>>();
    var checker = new AsteriskManagerHealthChecker(logger);
    
    var result = checker.CheckHealth(new List<AsteriskManagerProvider>(), DateTimeOffset.UtcNow);
    
    Assert.False(result.IsHealthy);
}
```

#### **3. Reutiliza��o de C�digo**
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

#### **4. Configura��o Flex�vel**
```csharp
// Diferentes estrat�gias de health check
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

#### **API P�blica Inalterada:**
```csharp
// M�todos existentes continuam funcionando
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

- ? **C�digo mais limpo** e organizado
- ? **Melhor testabilidade** com classes isoladas
- ? **Maior reutiliza��o** de componentes
- ? **Extensibilidade aprimorada** para novos cen�rios
- ? **Compatibilidade total** com c�digo existente
- ? **Arquitetura mais robusta** seguindo SOLID principles

A nova arquitetura transforma um arquivo monol�tico de 600+ linhas em m�ltiplos arquivos especializados, cada um com responsabilidades bem definidas, mantendo total compatibilidade com o c�digo existente! ??