# Treasury Transfers API - Remitee Backend Challenge

## 📝 Descripción del Proyecto
Esta aplicación resuelve la gestión de transferencias internas de fondos entre cuentas dentro de una tesorería centralizada. Garantiza que las transferencias de saldo sean **atómicas** (ocurren en su totalidad o no ocurre nada), **idempotentes** (se procesan de manera única basándose en una clave `operationId`) e implementa validaciones estrictas de saldo, estados de cuenta y tipos de cambio (FX) multimoneda.

Para detalles avanzados sobre las reglas de negocio o las decisiones técnicas de diseño, consulta:
* **[Especificación y Reglas de Negocio (docs/INSTRUCTIONS.md)](file:///d:/Repositories/challenges/challenge.remitee-treasury-transfers-api/docs/INSTRUCTIONS.md)**
* **[Detalle de Diseño y Arquitectura (docs/ARCHITECTURE.md)](file:///d:/Repositories/challenges/challenge.remitee-treasury-transfers-api/docs/ARCHITECTURE.md)**

---

## 🏗️ Arquitectura Aplicada
* **Clean Architecture**: División física de responsabilidades para mantener el dominio aislado de tecnologías de infraestructura.
* **Domain-Driven Design (DDD)**: Modelado de negocio basado en Entidades, Objetos de Valor e Invariantes del Dominio.
* **CQRS (Command Query Responsibility Segregation)**: Flujo de procesamiento de comandos desacoplado mediante el mediador `DispatchR`.

---

## 🛠️ Stack Tecnológico
* **Runtime & Framework**: .NET 10 / ASP.NET Core
* **Base de Datos**: Microsoft SQL Server 2022 (Dockerizado)
* **ORM & Persistencia**: Entity Framework Core / SQL Server
* **Mediador (CQRS)**: DispatchR
* **UI Interactiva & OpenAPI**: Scalar / Microsoft.AspNetCore.OpenApi (.NET 10 Nativo)
* **Contenedores**: Docker & Docker Compose
* **Pruebas**: xUnit, FluentAssertions y FakeItEasy

---

## 🚀 Prerrequisitos
* Tener instalado [Docker Desktop](https://www.docker.com/products/docker-desktop/) en tu máquina local.

---

## 🔍 Paso a Paso para Levantar y Probar el Proyecto

### 1. Levantar el Proyecto con Docker Compose
Abre tu terminal en la raíz del repositorio y ejecuta:
```bash
docker-compose up --build -d
```
Este comando construirá la imagen del servidor web .NET, levantará la base de datos de SQL Server en segundo plano y aplicará las migraciones y datos semilla (seed data) automáticamente.

### 2. Probar la API de forma Interactiva (Scalar UI)
Una vez que el contenedor esté corriendo:
* Ingresa a: **[http://localhost:8080/scalar/v1](http://localhost:8080/scalar/v1)** para abrir la interfaz interactiva.
* Podrás ver la documentación OpenAPI, los esquemas detallados y probar el endpoint `POST /transfers` directamente.