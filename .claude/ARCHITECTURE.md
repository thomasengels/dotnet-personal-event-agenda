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

A **use case** is a sealed class representing a single application operation. It exposes exactly one public method, named `ExecuteAsync`, returning a `Task` or `Task<T>`. Dependencies are injected via a primary constructor. Use cases are plain concrete classes — they are not exposed behind a use-case-specific interface; API controllers inject them directly. A use case may have private helper methods, but only one public method.

Example structure:

```text
Application/
└── UseCases/
    └── CreateOrderUseCase.cs
```

Example:

```csharp
public sealed class CreateOrderUseCase(IOrderRepository repository)
{
    public async Task<Order> ExecuteAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId);

        await repository.Save(order, ct);

        return order;
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

API endpoints inject **Application use cases**, not repositories or infrastructure implementations.

Example:

```csharp
public sealed class OrderController(CreateOrderUseCase createOrder) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderCommand command, CancellationToken ct)
    {
        var order = await createOrder.ExecuteAsync(command, ct);

        return Ok(order);
    }
}
```

The expected execution flow is:

```text
HTTP Request
    ↓
API Controller / Endpoint
    ↓
Application Use Case (ExecuteAsync)
    ↓
Domain
    ↓
Repository / Outbound Port
    ↓
Infrastructure Implementation
```

## Architectural Rules

When creating or modifying code, follow these rules:

1. **Use cases belong in `Application`.**
2. **A use case is a sealed class with exactly one public method, `ExecuteAsync`.** No use-case-specific interface is required.
3. **API endpoints depend on Application use cases directly.**
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
