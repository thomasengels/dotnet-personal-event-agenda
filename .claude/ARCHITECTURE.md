## Architecture

This project uses a **Hexagonal Architecture** with four main layers:

```text
API
Application
Domain
Infrastructure
```

Dependencies must always point inward toward the application/domain core.

```text
API ───────────────► Application ───────────────► Domain
                          ▲                         ▲
                          │                         │
Infrastructure ───────────┴─────────────────────────┘
```

The `Domain` layer must never depend on `Application`, `Infrastructure`, or `API`.

### Domain

The `Domain` layer contains the core business model and business rules.

Typical contents:

* Entities
* Aggregates
* Value Objects
* Domain Services
* Domain Events
* Domain-specific repository interfaces / outbound ports

Example:

```csharp
public interface IOrderRepository
{
    Task<Order?> GetById(OrderId id);
    Task Save(Order order);
}
```

Repository interfaces belong in `Domain` when they represent persistence requirements of the domain model.

The domain must not contain:

* Controllers or HTTP concepts
* EF Core or database implementations
* External API clients
* Application use cases
* Infrastructure-specific code
* Framework-specific integration logic

### Application

The `Application` layer contains the application's **use cases and orchestration logic**.

Both the use case interface and its implementation belong in `Application`.

Example structure:

```text
Application/
└── Orders/
    └── CreateOrder/
        ├── ICreateOrderUseCase.cs
        ├── CreateOrderUseCase.cs
        └── CreateOrderCommand.cs
```

Example:

```csharp
public interface ICreateOrderUseCase
{
    Task Execute(CreateOrderCommand command);
}
```

```csharp
public sealed class CreateOrderUseCase : ICreateOrderUseCase
{
    private readonly IOrderRepository _repository;

    public CreateOrderUseCase(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(CreateOrderCommand command)
    {
        var order = Order.Create(command.CustomerId);

        await _repository.Save(order);
    }
}
```

Application use cases may:

* Load domain objects through repository interfaces
* Invoke domain behavior
* Coordinate multiple domain objects
* Persist state through repository interfaces
* Coordinate external dependencies through ports
* Define transaction/application boundaries
* Return application-level results

Application use cases should **orchestrate domain behavior rather than implement business rules themselves**.

Conceptually:

```text
load state
    ↓
invoke domain behavior
    ↓
persist state
    ↓
return result
```

### Infrastructure

The `Infrastructure` layer implements outbound ports defined by the inner layers.

Typical contents:

* Repository implementations
* EF Core / database access
* Message brokers
* Kafka / RabbitMQ clients
* External HTTP clients
* File/blob storage
* Third-party integrations

Example:

```csharp
public sealed class SqlOrderRepository : IOrderRepository
{
    // EF Core implementation
}
```

Infrastructure may depend on `Domain` and `Application`.

`Domain` and `Application` must never depend on concrete infrastructure implementations.

### API

The `API` layer is an inbound adapter.

Typical contents:

* Controllers
* Minimal API endpoints
* HTTP request/response models
* Authentication/authorization integration
* HTTP-specific validation and mapping

API endpoints inject **Application use case interfaces**, not repositories or infrastructure implementations.

Example:

```csharp
public sealed class OrderController : ControllerBase
{
    private readonly ICreateOrderUseCase _createOrder;

    public OrderController(ICreateOrderUseCase createOrder)
    {
        _createOrder = createOrder;
    }
}
```

The expected execution flow is:

```text
HTTP Request
    ↓
API Controller / Endpoint
    ↓
Application Use Case Interface
    ↓
Application Use Case Implementation
    ↓
Domain
    ↓
Repository / Outbound Port
    ↓
Infrastructure Implementation
```

Or, simplified:

```text
REST
  ↓
Application inbound port
  ↓
Application use case
  ↓
Domain
  ↓
Outbound port
  ↓
Infrastructure
```

## Architectural Rules

When creating or modifying code, follow these rules:

1. **Use case interfaces belong in `Application`.**
2. **Use case implementations belong in `Application`.**
3. **API endpoints depend on Application use case interfaces.**
4. **API endpoints must not directly use repositories.**
5. **Repository implementations belong in `Infrastructure`.**
6. **Repository interfaces may belong in `Domain` when they represent a domain persistence abstraction.**
7. **Domain must remain independent of Application, Infrastructure, and API.**
8. **Application may depend on Domain, but not on Infrastructure.**
9. **Infrastructure may depend on Application and Domain to implement their ports.**
10. **Business rules belong in Domain objects whenever possible.**
11. **Use cases coordinate business behavior; they should not become containers for domain logic.**
12. **Framework-specific code must remain outside the Domain layer.**
13. **Id-driven entities must inherit from a common abstract entity base.** Any domain entity identified by an `Id` (as opposed to a value object) inherits from a shared abstract base (e.g. `Entity<TId>`) that owns identity and identity-based equality, rather than each entity hand-rolling its own `Id` property and equality members.

When deciding where new code belongs, prefer the layer based on its responsibility rather than convenience.
