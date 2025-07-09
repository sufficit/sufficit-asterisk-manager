```markdown
# ManagerEventSubscriptions Clean Architecture with Separated Event Building

## Overview

A nova arquitetura implementa uma separa��o clara de responsabilidades, dividindo a l�gica de constru��o de eventos da l�gica de subscription/dispatch. Agora temos duas classes especializadas:

1. **ManagerEventBuilder** - Respons�vel por constru��o e parsing de eventos (est�tico)
2. **ManagerEventSubscriptions** - Respons�vel por subscriptions e dispatching (inst�ncia)

## Problema Original Resolvido

- **"ManagerEventSubscriptions was not properly disposed"** warnings
- **"Event consumer task was canceled gracefully"** durante inicializa��o
- Conflitos de disposal quando inst�ncias eram compartilhadas
- L�gica complexa misturada em uma �nica classe

## Nova Arquitetura: Separa��o de Responsabilidades

### 1. ManagerEventBuilder (Est�tico)

**Responsabilidades:**
- Descoberta de tipos de eventos em assemblies
- Registro de classes de eventos 
- Constru��o de inst�ncias de eventos a partir de packets AMI
- Parsing de Action IDs e processamento de atributos
- Cache de construtores para performance

```csharp
// Exemplo de uso
var eventObject = ManagerEventBuilder.Build(packet);
var eventKey = ManagerEventBuilder.GetEventKey<SomeEvent>();
ManagerEventBuilder.RegisterUserEventClass(typeof(CustomEvent));
```

**Caracter�sticas:**
- Thread-safe e otimizado para performance
- Cache global de tipos descobertos
- M�todos utilit�rios para diagn�stico
- Sem estado de inst�ncia (totalmente est�tico)

### 2. ManagerEventSubscriptions (Inst�ncia)

**Responsabilidades:**
- Gerenciamento de subscriptions de eventos
- Dispatching de eventos para handlers
- Producer-consumer pattern com channels
- Lifecycle management (disposal)

```csharp
// Exemplo de uso
var subscription = events.On<SomeEvent>(OnSomeEvent);
events.Dispatch(sender, eventInstance);
events.FireAllEvents = true;
```

**Caracter�sticas:**
- Clean disposal behavior (sem complexidade de ownership)
- High-performance event dispatching
- Suporte a handlers abstratos e espec�ficos
- Gest�o autom�tica de cleanup

## Fluxo Completo de Eventos

```mermaid
graph TD
    A[AMI Packet] --> B[ManagerEventBuilder.Build]
    B --> C[Event Instance]
    C --> D[ManagerEventSubscriptions.Dispatch]
    D --> E[Event Handlers]
```

### Implementa��o no ManagerConnection

```csharp
// No ProcessPacketQueueAsync
if (packet.ContainsKey("event"))
{
    // 1. Build using static builder
    var eventObject = ManagerEventBuilder.Build(packet);
    
    // 2. Dispatch using active subscription system
    if (eventObject != null)
        _activeEvents.Dispatch(this, eventObject.Event);
}
```

## Benef�cios da Separa��o

### ? **Separation of Concerns**
- **ManagerEventBuilder**: Foca em constru��o e parsing
- **ManagerEventSubscriptions**: Foca em subscription e dispatch
- Cada classe tem uma responsabilidade bem definida

### ? **Performance Otimizada**
- Cache global de tipos (n�o duplicado por inst�ncia)
- Constructors cachados estaticamente
- Menos overhead de memory por conex�o

### ? **Facilidade de Manuten��o**
- L�gica de building centralizada e test�vel
- Subscription logic isolado e reutiliz�vel
- Menos complexidade em cada classe

### ? **Flexibilidade**
- Event building pode ser usado independentemente
- Subscription system pode ser usado com qualquer fonte
- Extensibilidade clara para novos tipos de eventos

### ? **Debugging e Diagnostics**
- M�todos espec�ficos para diagn�stico em ManagerEventBuilder
- Logs separados para building vs dispatching
- Estat�sticas sobre tipos registrados

## M�todos de Diagn�stico

```csharp
// Verificar quantos tipos est�o registrados
int count = ManagerEventBuilder.RegisteredEventClassCount;

// Ver todos os event keys registrados
var keys = ManagerEventBuilder.RegisteredEventKeys;

// Verificar se um tipo espec�fico est� registrado
bool registered = ManagerEventBuilder.IsEventKeyRegistered("hangup");
```

## Cen�rios de Uso

### Cen�rio 1: Conex�o Individual
```csharp
var connection = new ManagerConnection(parameters);
// Usa eventos internos automaticamente
// ManagerEventBuilder � usado transparentemente para build
// ManagerEventSubscriptions gerencia subscriptions
```

### Cen�rio 2: Servi�o Compartilhado (AMIService)
```csharp
// AMIService cria subscription compartilhado
var sharedEvents = new ManagerEventSubscriptions();

// Conex�es usam o compartilhado
connection1.Use(sharedEvents, disposable: false);
connection2.Use(sharedEvents, disposable: false);

// ManagerEventBuilder permanece global e compartilhado
```

### Cen�rio 3: Custom Event Registration
```csharp
// Registrar globalmente (afeta todos)
ManagerEventBuilder.RegisterUserEventClass(typeof(MyCustomEvent));

// Ou registrar em uma inst�ncia espec�fica
events.RegisterUserEventClass(typeof(MyCustomEvent)); // delega para builder
```

## Compara��o: Antes vs Depois

### Antes (Monol�tico)
```csharp
public class ManagerEventSubscriptions
{
    // Building logic mixed with subscription logic
    public static ManagerEventGeneric Build(...)
    public IDisposable On<T>(...)
    public void Dispatch(...)
    
    // Complex ownership tracking
    private static Dictionary<string, ConstructorInfo> _registeredEventClasses;
    private ConcurrentDictionary<string, ManagerInvokable> _handlers;
}
```

### Depois (Separado)
```csharp
public static class ManagerEventBuilder
{
    // Pure building and parsing logic
    public static ManagerEventGeneric Build(...)
    public static void RegisterUserEventClass(...)
    public static string GetEventKey(...)
}

public class ManagerEventSubscriptions
{
    // Pure subscription and dispatching logic
    public IDisposable On<T>(...)
    public void Dispatch(...)
    // Clean disposal without ownership complexity
}
```

## Resultados

- **Zero warnings** de disposal inadequado
- **Zero conflitos** de ownership
- **C�digo mais modular** e f�cil de entender
- **Performance melhorada** com cache global
- **Facilidade de testes** com responsabilidades separadas
- **Extensibilidade** para futuras funcionalidades

A nova arquitetura resolve completamente os problemas originais e fornece uma base s�lida e maint�vel para o sistema AMI, seguindo princ�pios de design limpo e separa��o de responsabilidades.
```