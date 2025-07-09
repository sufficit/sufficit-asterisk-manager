```markdown
# ManagerEventSubscriptions Clean Architecture

## Overview

A nova arquitetura implementa uma abordagem limpa e elegante para gerenciar eventos AMI, separando claramente as responsabilidades e eliminando problemas de disposal.

## Problema Original

- **"ManagerEventSubscriptions was not properly disposed"** warnings
- **"Event consumer task was canceled gracefully"** durante inicializa��o
- Conflitos de disposal quando a mesma inst�ncia era compartilhada entre m�ltiplas conex�es
- L�gica complexa de ownership

## Solu��o: Arquitetura Internal + External

### Princ�pios da Nova Arquitetura

1. **ManagerConnection sempre tem um evento interno pr�prio**
   - Criado automaticamente no construtor
   - Sempre descartado quando a conex�o � encerrada
   - Nunca null, sempre dispon�vel

2. **Eventos externos s�o opcionais**
   - Fornecidos via m�todo `Use()`
   - Nunca descartados pela conex�o
   - Gerenciados pelo criador (ex: AMIService)

3. **Separa��o clara de responsabilidades**
   - Conex�o: gerencia seu pr�prio evento interno
   - Servi�o: gerencia eventos externos compartilhados
   - Sem conflitos de ownership

## Implementa��o

### ManagerConnection

```csharp
public class ManagerConnection
{
    // Evento interno - sempre existe, sempre descartado
    private readonly ManagerEventSubscriptions _internalEvents;
    
    // Evento externo - opcional, nunca descartado
    private IManagerEventSubscriptions? _externalEvents;
    
    // Evento ativo - interno por padr�o, externo se configurado
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

## M�todos de Controle

### Use() - Trocar para Evento Externo
```csharp
// Usar evento externo (compartilhado)
connection.Use(sharedEvents, disposable: false); // Recomendado
connection.Use(sharedEvents, disposable: true);  // Se desejar disposal autom�tico
```

### UseInternal() - Voltar para Evento Interno
```csharp
// Voltar para evento interno
connection.UseInternal(disposeExternal: false); // Manter externo vivo
connection.UseInternal(disposeExternal: true);  // Descartar externo
```

## Fluxo de Eventos

1. **Build**: Sempre usa o evento interno (tem m�todo Build)
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

## Cen�rios de Uso

### Cen�rio 1: Conex�o Individual
```csharp
var connection = new ManagerConnection(parameters);
// Usa evento interno automaticamente
connection.Events.On<SomeEvent>(OnSomeEvent);
// Disposal autom�tico quando connection.Dispose()
```

### Cen�rio 2: Servi�o Compartilhado
```csharp
// AMIService cria evento compartilhado
var sharedEvents = new ManagerEventSubscriptions();
sharedEvents.On<SomeEvent>(OnSomeEvent);

// Conex�es usam o evento compartilhado
connection1.Use(sharedEvents, disposable: false);
connection2.Use(sharedEvents, disposable: false);

// AMIService descarta quando termina
await sharedEvents.DisposeAsync();
```

## Benef�cios

? **Sem warnings de disposal** - Ownership claro e simples
? **Sem conflitos** - Cada inst�ncia tem responsabilidade definida  
? **Flexibilidade** - Pode usar interno ou externo conforme necess�rio
? **Simplicidade** - Sem l�gica complexa de ownership
? **Robustez** - Sempre h� um evento interno como fallback
? **Performance** - Compartilhamento eficiente quando necess�rio

## Compara��o: Antes vs Depois

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
- **C�digo mais limpo** e f�cil de entender
- **Arquitetura mais robusta** com separa��o clara de responsabilidades
- **Melhor performance** com compartilhamento eficiente

A nova arquitetura resolve completamente os problemas de disposal mantendo a flexibilidade e performance necess�rias para o sistema AMI.
```