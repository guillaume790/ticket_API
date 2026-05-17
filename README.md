# 🎟️ TicketApi

Projet d’API REST en **.NET 10**, développé avec **Visual Studio 2026**.  
Cette API a pour objectif de gérer des tickets (support, tâches, incidents).

---

## 🧰 Environnement de développement

- Visual Studio 2026  
- .NET 10 SDK  
- Windows 11  
- GitHub Desktop pour la gestion du versionnement  
- SQLite pour le stockage local  
- Entity Framework Core pour l’accès aux données

---

## 🚀 Technologies utilisées

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core
- SQLite
- Swagger / OpenAPI

---

## 📌 À propos du projet

Ce dépôt démarre avec une base minimale et sera enrichi au fil du développement.  
Chaque nouvelle fonctionnalité sera développée dans une branche dédiée, puis fusionnée dans `main`.

Le README sera mis à jour progressivement dans chaque branche pour refléter l’évolution du projet.

---

## 📁 Structure du projet

Principaux dossiers/fichiers : Controllers/, Data/, Models/, Program.cs, appsettings.json, ticket_API.csproj

---

## ⚙️ Installation et exécution

Prérequis : .NET 10 SDK et un terminal (PowerShell).

1. Restaurer les paquets :

   ```bash
   dotnet restore
   ```

2. Créer les migrations  :

   ```bash
   dotnet ef migrations add InitialCreate
   ```

3. Mettre à jour la base de données :

   ```bash
   dotnet ef database update
   ```

4. Lancer l'application :

   ```bash
   dotnet run
   ```


---

## 🗄️ Base de données et Entity Framework Core

- Le projet utilise SQLite via Entity Framework Core.
- La chaîne de connexion se situe dans appsettings.json sous ConnectionStrings:DefaultConnection.
- AppDbContext définit les DbSet pour Ticket, User et Event.
- AppDbContextFactory fournit une usine pour permettre à `dotnet ef` d'instancier le DbContext en design-time.

---

## 📦 Modèles (entités)

- Ticket : Id, Title, Description, Price, EventId, UserId, CreatedAt
- User : Id, FirstName, LastName, Email, CreatedAt
- Event : Id, Name, Date, Location, Tickets (liste)

---

## 🔌 Endpoints principaux

🎫 Events
GET /events
Liste tous les événements.

GET /events/{id}
Récupère un événement par ID.

POST /events
Crée un événement.

PUT /events/{id}
Modifie un événement.

DELETE /events/{id}
Supprime un événement.

---

## 🎟 Tickets

### GET /tickets
Liste tous les tickets.

### GET /tickets/{id}
Récupère un ticket par ID.

### POST /tickets
Crée un ticket.

### PUT /tickets/{id}
Modifie un ticket.

### DELETE /tickets/{id}
Supprime un ticket.

Les tickets sont liés à un événement via EventId.
La relation Event → Tickets est gérée par Entity Framework Core.

---

👤 Users

GET /users
Retourne la liste de tous les utilisateurs.

GET /users/{id}
Retourne un utilisateur spécifique via son ID.

POST /users
Crée un nouvel utilisateur.

PUT /users/{id}
Modifie un utilisateur existant.

DELETE /users/{id}
Supprime un utilisateur.

---

🔐 Authentification JWT
L’API utilise un système d’authentification basé sur JSON Web Tokens (JWT).

📝 Register
POST /auth/register

🔑 Login
POST /auth/login


🔗 Relations
Un User possède plusieurs Tickets

Un Ticket appartient à un User

Un Ticket appartient à un Event

Un Event possède plusieurs Tickets

