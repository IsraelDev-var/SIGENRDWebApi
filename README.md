# Arquitectura Backend - Clean Arquitecture (Onion)
📂 Solution: ExploradorElectricoRD
│
├── 📂 src
│   │
│   ├── 🟢 1. Domain  (Class Library)
│   │   ├── Common          # Entidad Base (Id, CreatedAt, UpdatedAt)
│   │   ├── Entities        # Transformer, Request, NetMeteringSimulation
│   │   ├── Enums           # RequestStatus, UserRole, VoltageLevel
│   │   ├── Events          # Domain Events (ej. RequestApprovedEvent)
│   │   ├── ValueObjects    # GeoLocation (Lat/Lng), Address
│   │   └── Interfaces      # IRepository<T>, IDomainService
│   │
│   ├── 🔴 2. Application (Class Library)
│   │   ├── Common
│   │   │   ├── Behaviors       # Validaciones automáticas (Pipelines MediatR)
│   │   │   ├── Interfaces      # IEmailService, ICurrentUserService, IStorageService
│   │   │   └── Exceptions      # NotFoundException, ValidationException
│   │   ├── Features            # ORGANIZADO POR MÓDULOS (Vertical Slices)
│   │   │   ├── Transformers
│   │   │   │   ├── Queries     # GetTransformersInBounds, GetTransformerById
│   │   │   │   └── DTOs        # TransformerDto
│   │   │   ├── Requests
│   │   │   │   ├── Commands    # CreateRequestCommand, ApproveRequestCommand
│   │   │   │   ├── Queries     # GetMyRequests
│   │   │   │   └── Validators  # CreateRequestValidator (FluentValidation)
│   │   │   └── Simulations
│   │   └── Mappings        # AutoMapper Profiles
│   │
│   ├── 🔵 3. Infrastructure (Class Library)
│   │   ├── Persistence
│   │   │   ├── Contexts        # ApplicationDbContext (EF Core)
│   │   │   ├── Configurations  # EntityConfigs (Fluent API & PostGIS setup)
│   │   │   ├── Migrations      # Archivos de migración SQL
│   │   │   └── Repositories    # Implementación de repositorios
│   │   ├── Services
│   │   │   ├── Azure           # BlobStorageService, KeyVaultService
│   │   │   ├── Email           # SendGridService
│   │   │   └── Identity        # IdentityService (JWT Logic)
│   │   └── Files               # Lógica para procesar PDFs/Excels
│   │
│   └── 🟡 4. WebAPI (ASP.NET Core 8 Web API)
│       ├── Controllers     # Endpoints limpios (Solo llaman a MediatR)
│       ├── Middlewares     # GlobalErrorHandler, RateLimiting
│       ├── Extensions      # Inyección de dependencias (ServiceCollection)
│       ├── appsettings.json
│       └── Program.cs
│
└── 📂 tests (¡Obligatorio para Calidad!)
    ├── UnitTests           # Pruebas de lógica de negocio (Application)
    └── IntegrationTests    # Pruebas con BD en memoria (Infrastructure)
