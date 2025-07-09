```markdown
# ManagerEventSubscriptions Clean Architecture

## Overview

A nova arquitetura implementa uma abordagem limpa e elegante para gerenciar eventos AMI, separando claramente as responsabilidades e eliminando problemas de disposal.

## Problema Original

- **"ManagerEventSubscriptions was not properly disposed"** warnings
- **"Event consumer task was canceled gracefully"** durante inicialização
- Conflitos de disposal quando a mesma instância era compartilhada entre múltiplas conexões
- Lógica complexa de ownership

## Solução: Arquitetura Internal + External

### Princípios da Nova Arquitetura

1. **ManagerConnection sempre tem um evento interno próprio**
   - Criado automaticamente no construtor
   - Sempre descartado quando a conexão é encerrada
   - Nunca null, sempre disponível

2. **Eventos externos são opcionais**
   - Fornecidos via método `Use()`
   - Nunca descartados pela conexão
   - Gerenciados pelo criador (ex: AMIService)

3. **Separação clara de responsabilidades**
   - Conexão: gerencia seu próprio evento interno
   - Serviço: gerencia eventos externos compartilhados
   - Sem conflitos de ownership

## Implementação

### ManagerConnection

```csharp
public class ManagerConnection
{
    // Evento interno - sempre existe, sempre descartado
    private readonly ManagerEventSubscriptions _internalEvents;
    
    // Evento externo - opcional, nunca descartado
    private IManagerEventSubscriptions? _externalEvents;
    
    // Evento ativo - interno por padrão, externo se configurado
    private IManagerEventSubscriptions _activeEvents;
    
    public IManagerEventSubscriptions Events => _activeEvents;
}
```

### AMIService

```csharp
public class AMIService : AsteriskManagerService
{
    // Evento externo compartilhado - owned by AMIService
    private readonly ManagerEventSubscriptions _externalEvents;
    
    protected override AsteriskManagerProvider CreateProvider(AMIProviderOptions options)
    {
        var provider = base.CreateProvider(options);
        
        // Configure connections to use our external shared events
        provider.Connection?.Use(_externalEvents, disposable: false);
        
        return provider;
    }
}
```

## Métodos de Controle

### Use() - Trocar para Evento Externo
```csharp
// Usar evento externo (compartilhado)
connection.Use(sharedEvents, disposable: false); // Recomendado
connection.Use(sharedEvents, disposable: true);  // Se desejar disposal automático
```

### UseInternal() - Voltar para Evento Interno
```csharp
// Voltar para evento interno
connection.UseInternal(disposeExternal: false); // Manter externo vivo
connection.UseInternal(disposeExternal: true);  // Descartar externo
```

## Fluxo de Eventos

1. **Build**: Sempre usa o evento interno (tem método Build)
2. **Dispatch**: Sempre usa o evento ativo (interno ou externo)
3. **Subscription**: Sempre usa o evento ativo

```csharp
// No ProcessPacketQueueAsync
var eventObject = _internalEvents.Build(packet);    // Build internal
if (eventObject != null)
{
    _activeEvents.Dispatch(this, eventObject.Event); // Dispatch active
}
```

## Cenários de Uso

### Cenário 1: Conexão Individual
```csharp
var connection = new ManagerConnection(parameters);
// Usa evento interno automaticamente
connection.Events.On<SomeEvent>(OnSomeEvent);
// Disposal automático quando connection.Dispose()
```

### Cenário 2: Serviço Compartilhado
```csharp
// AMIService cria evento compartilhado
var sharedEvents = new ManagerEventSubscriptions();
sharedEvents.On<SomeEvent>(OnSomeEvent);

// Conexões usam o evento compartilhado
connection1.Use(sharedEvents, disposable: false);
connection2.Use(sharedEvents, disposable: false);

// AMIService descarta quando termina
await sharedEvents.DisposeAsync();
```

## Benefícios

? **Sem warnings de disposal** - Ownership claro e simples
? **Sem conflitos** - Cada instância tem responsabilidade definida  
? **Flexibilidade** - Pode usar interno ou externo conforme necessário
? **Simplicidade** - Sem lógica complexa de ownership
? **Robustez** - Sempre há um evento interno como fallback
? **Performance** - Compartilhamento eficiente quando necessário

## Comparação: Antes vs Depois

### Antes (Complexo)
```csharp
// Ownership tracking, complex disposal logic
var events = new ManagerEventSubscriptions(externalOwnership: true);
if (!events.IsExternallyOwned) events.Dispose(); // ??
await events.ForceDisposeAsync(); // Confuso
```

### Depois (Simples)
```csharp
// Clear ownership
var sharedEvents = new ManagerEventSubscriptions(); // AMIService owns
connection.Use(sharedEvents, disposable: false);    // Connection uses
await sharedEvents.DisposeAsync();                  // AMIService disposes
```

## Resultados

- **Zero warnings** de disposal inadequado
- **Zero conflitos** de ownership
- **Código mais limpo** e fácil de entender
- **Arquitetura mais robusta** com separação clara de responsabilidades
- **Melhor performance** com compartilhamento eficiente

A nova arquitetura resolve completamente os problemas de disposal mantendo a flexibilidade e performance necessárias para o sistema AMI.
```