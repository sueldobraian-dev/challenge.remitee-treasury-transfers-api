# 🏗️ Decisiones de Arquitectura y Enfoque de la Solución

Este documento detalla las decisiones técnicas y de arquitectura implementadas en la solución de la API de transferencias de tesorería.

## 1. Clean Architecture & DDD (Domain-Driven Design)
Para garantizar la separación de conceptos y proteger el núcleo del negocio, la aplicación se divide en:
* **Challenge.Domain (Dominio)**: Contiene entidades de negocio puras (`Account`, `LedgerTransaction`, `Currency`, `Money`) y excepciones de negocio específicas. No posee dependencias tecnológicas externas.
* **Challenge.Application (Aplicación)**: Contiene los casos de uso (`CreateTransferCommand`, `CreateTransferCommandHandler`), abstracciones de dependencias y el mediador (`DispatchR`).
* **Challenge.Infrastructure (Infraestructura)**: Implementa la persistencia de datos mediante Entity Framework Core, repositorios y el patrón Unit of Work sobre SQL Server.
* **Challenge.InfrastructureBootstrap (Bootstrap)**: Orquesta la inyección de dependencias centralizada y la configuración del pipeline del middleware.
* **Challenge.API (Presentación)**: Define los controladores de endpoints HTTP y la autogeneración de OpenAPI.

## 2. CQRS con DispatchR
Se implementa una separación estricta de responsabilidades (Command Query Responsibility Segregation) utilizando un mediador minimalista en memoria llamado **DispatchR**, optimizado para despachar comandos desacoplados de la presentación de la API.

## 3. Idempotencia y Atomicidad
* **Idempotencia**: Se garantiza mediante un índice único en la base de datos sobre el campo `OperationId` de la tabla `LedgerTransactions` y validaciones previas en memoria en el manejador.
* **Atomicidad**: Se implementa a través del patrón Unit of Work. Los saldos de las cuentas y el registro de la transacción se guardan de forma atómica en una única transacción de base de datos (`SaveChangesAsync`).

## 4. Control de Concurrencia (OCC)
Para mitigar el problema de doble gasto o condiciones de carrera concurrentes:
* Se configuró control de concurrencia optimista (Optimistic Concurrency Control) mediante una propiedad oculta de versión `RowVersion` en EF Core para la tabla de cuentas (`Accounts`).
* Si dos peticiones intentan debitar de la misma cuenta simultáneamente, una fallará con `DbUpdateConcurrencyException`, impidiendo inconsistencias financieras.

---

## 📦 Estructura del Directorio
El código de la solución se organiza dentro de las siguientes carpetas limpias:

```text
📂 challenge.remitee-treasury-transfers-api
 ├── 📂 .agents                   # Reglas y Skill del asistente de IA
 ├── 📂 docs                      # Documentación profunda y reglas del negocio
 ├── 📂 src
 │    ├── 📂 Challenge.Domain     # Entidades, Value Objects, Domain Events y abstracción de repositorios.
 │    ├── 📂 Challenge.Application# Casos de uso (Commands/Queries), validadores y handlers de DispatchR.
 │    ├── 📂 Challenge.Infrastructure# Persistencia (EF Core, DbContext), Repositorios y Unit of Work.
 │    ├── 📂 Challenge.Infrastructure.Bootstrap # Orquestación del bootstrap de DI, pipeline y inicialización.
 │    └── 📂 Challenge.API        # Capa de presentación (Controladores, OpenAPI nativo y Dockerfile).
 ├── docker-compose.yml           # Definición de servicios para API y SQL Server.
 └── Challenge.slnx               # Solución de C# (.NET)
```
