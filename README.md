# Treasury Transfers API - Remitee Backend Challenge

Este repositorio contiene la resolución del challenge técnico para **Remitee**, diseñado para gestionar transferencias internas entre cuentas de manera **atómica e idempotente**. 

Para leer el detalle profundo de las reglas de negocio, consulta la guía de diseño en: **[Especificación y Reglas de Negocio (docs/INSTRUCTIONS.md)](file:///d:/Repositories/challenges/challenge.remitee-treasury-transfers-api/docs/INSTRUCTIONS.md)**.

---

## 🏗️ Decisiones de Arquitectura y Enfoque Enterprise-grade

Aunque el enunciado del challenge sugiere la posibilidad de usar un almacenamiento *in-memory* básico, en un escenario real de tecnología financiera (FinTech) esto es inviable. Para demostrar un nivel de ingeniería avanzado, se diseñó e implementó una solución basada en:

1. **Clean Architecture & DDD (Domain-Driven Design):** 
   - Aislamiento de las reglas de negocio en una capa de **Dominio** pura, libre de dependencias tecnológicas.
   - Uso de **Entidades**, **Agregados** y **Objetos de Valor (Value Objects)** para modelar la semántica financiera de manera estricta.
   - Ejecución de validaciones y lógica de negocio dentro de la frontera del dominio, evitando "modelos anémicos".
2. **CQRS con DispatchR:**
   - Separación estricta de las operaciones de escritura (Commands) y lectura (Queries) mediante un flujo desacoplado.
   - Procesamiento de transferencias a través de un `CreateTransferCommand` manejado por DispatchR, coordinando la transacción de base de datos de manera eficiente.
3. **Persistencia Real con SQL Server (Dockerizado):**
   - Garantía de **ACID** (Atomicidad, Consistencia, Aislamiento y Durabilidad) mediante el uso de transacciones SQL Server reales administradas con Entity Framework Core.
   - Implementación del patrón **Repository** y **Unit of Work** para asegurar que los débitos, créditos e inserciones del Ledger ocurran de forma atómica.
   - Implementación de **Optimistic Concurrency Control** (control de concurrencia optimista) utilizando un token de versión en la entidad de cuentas para evitar condiciones de carrera (*race conditions* / *double-spend*).
4. **Idempotencia Transaccional:**
   - Almacenamiento del `operationId` proporcionado por el cliente con un índice único en la tabla `LedgerTransactions`, asegurando la no existencia de duplicados a nivel de almacenamiento y de aplicación.

---

## 🛠️ Stack Tecnológico

* **Runtime & Framework:** .NET 10 / Minimal APIs
* **Base de Datos:** Microsoft SQL Server 2022
* **Mapeador ORM:** Entity Framework Core
* **Patrón de Comportamiento:** DispatchR (CQRS)
* **Documentación:** OpenAPI nativo (.NET 10)
* **Contenedores y Orquestación:** Docker & Docker Compose
* **Pruebas:** xUnit, FluentAssertions y Moq

---

## 📦 Estructura del Proyecto

El código de la solución se organiza dentro del directorio `src/` en las siguientes capas limpias:

```text
📂 challenge.remitee-treasury-transfers-api
 ├── 📂 .agents                   # Reglas y Skill del asistente de IA
 ├── 📂 docs                      # Documentación profunda y reglas del negocio
 ├── 📂 src
 │    ├── 📂 Challenge.Domain     # Entidades, Value Objects, Domain Events y abstracción de repositorios.
 │    ├── 📂 Challenge.Application# Casos de uso (Commands/Queries), validadores y handlers de DispatchR.
 │    ├── 📂 Challenge.Infrastructure# Persistencia (EF Core, DbContext), Repositorios y Unit of Work.
 │    ├── 📂 Challenge.InfrastructureBootstrap # Orquestación del bootstrap de DI, pipeline y inicialización.
 │    └── 📂 Challenge.API        # Capa de presentación (Controladores, OpenAPI nativo y Dockerfile).
 ├── docker-compose.yml           # Definición de servicios para API y SQL Server.
 └── Challenge.slnx               # Solución de C# (.NET)
```

---

## 🚀 Guía de Inicio Rápido (Quick Start)

Para levantar el proyecto y la base de datos de manera automática con todas sus migraciones y datos semilla (seed data) precargados, sigue los siguientes pasos:

### Prerrequisitos
- Tener instalado [Docker Desktop](https://www.docker.com/products/docker-desktop/) y habilitado el motor de Docker.

### Levantar la Solución
Ejecuta el siguiente comando en la raíz del repositorio:

```bash
docker-compose up --build -d
```

Este comando descargará la imagen de SQL Server, compilará el contenedor de la API de .NET, levantará ambos servicios en una red común y aplicará automáticamente las migraciones para crear la base de datos y sembrar las cuentas iniciales.

---

## 🔍 Cómo probar la API (OpenAPI)

Una vez que el contenedor esté corriendo, la API estará expuesta en el puerto `8080` de tu host local.

* **OpenAPI Spec:** Se puede acceder al documento de especificación OpenAPI en **[http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json)**.
* **Prueba de Endpoints:** Se puede interactuar con el endpoint `POST /transfers` utilizando clientes HTTP tradicionales (tales como Postman, cURL, o herramientas de desarrollo locales) o interfaces interactivas compatibles con OpenAPI.

### Ejemplo de Request (Con Conversión FX)

```json
{
  "operationId": "a3f1c9d2-1111-2222-3333-444444444444",
  "sourceAccountId": "ACC-USD-1",
  "targetAccountId": "ACC-ARS-1",
  "amount": 100.00,
  "currency": "USD",
  "fx": 1000.00
}
```

### Ejemplo de Respuesta Exitosa (`200 OK` / `201 Created`)

```json
{
  "id": "8fa8d39c-5555-6666-7777-888888888888",
  "operationId": "a3f1c9d2-1111-2222-3333-444444444444",
  "status": "COMPLETED",
  "sourceAccountId": "ACC-USD-1",
  "targetAccountId": "ACC-ARS-1",
  "amountDebited": 100.00,
  "amountCredited": 100000.00,
  "createdAt": "2026-05-28T13:00:00.000Z"
}
```