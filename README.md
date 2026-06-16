# SchoolLink AI — EduConnect

Full-stack: .NET 8 REST API + Angular

## Architecture

### Backend — N-Tier Architecture (4 Layers)
- Project.API    → WEB Layer (Controllers, Middleware)
- Project.BLL    → Business Logic (Services, DTOs, Validators)
- Project.DAL    → Data Access (Repositories, UnitOfWork, EF Core)
- Project.Domain → Domain (Entities, Enums, Exceptions)

### Frontend — Angular Feature-based
- core/     → Singleton services, guards, interceptors
- shared/   → Reusable components, pipes, directives
- features/ → Feature modules (web / bbl / dal layers)
- layouts/  → App layouts
- pages/    → Standalone pages

## Run Backend
cd backend
dotnet restore
dotnet run --project Project.API

## Run Frontend
cd frontend
npm install
ng serve
