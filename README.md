================================================================================
                    MISTERTICKET - SYSTÈME DE RÉSERVATION
                           ROLAND GARROS 2026
================================================================================

DESCRIPTION
-----------
Plateforme web de réservation de billets pour les matchs de Roland Garros.
Projet académique réalisé dans le cadre du cours de développement Web.

Étudiant : Harith Lemti
HELB ILYA PRIGOGINE 
Date : 5 Janvier 2026

================================================================================
FONCTIONNALITÉS
================================================================================

UTILISATEUR
-----------
- Authentification sécurisée avec JWT (JSON Web Token)
- Visualisation des événements de Roland Garros
- Carte interactive du terrain terre battue avec sections colorées
- Sélection des places en temps réel
- Réservation temporaire avec expiration automatique (15 minutes)
- Paiement simulé avec confirmation
- Génération de billets PDF avec QR code unique
- Gestion des réservations (historique, annulation, téléchargement)

TECHNIQUE
---------
- Mise à jour temps réel avec SignalR
- Design responsive aux couleurs officielles de Roland Garros
- Sécurité : hashage des mots de passe, tokens JWT
- Service d'expiration automatique des réservations
- Notifications de confirmation (simulation)

================================================================================
TECHNOLOGIES UTILISÉES
================================================================================

BACKEND (.NET)
--------------
- .NET 8 - Framework principal
- Entity Framework Core - ORM pour la base de données
- SQL Server - Base de données relationnelle
- JWT (JSON Web Token) - Authentification sécurisée
- SignalR - Communication temps réel
- QuestPDF - Génération de billets PDF
- QRCoder - Génération de QR codes

FRONTEND (ANGULAR)
------------------
- Angular 17 - Framework frontend
- TypeScript - Langage de programmation
- Bootstrap 5 - Framework CSS
- RxJS - Programmation réactive
- HttpClient - Communication avec l'API

OUTILS DE DÉVELOPPEMENT
------------------------
- Visual Studio 2022 - IDE backend
- Visual Studio Code - Éditeur frontend
- Swagger - Documentation API
- Git - Gestion de versions

================================================================================
STRUCTURE DU PROJET
================================================================================

MisterTicket/
│
├── Backend/                    API .NET
│   ├── Controllers/            Contrôleurs API
│   ├── Models/                 Modèles de données
│   ├── Data/                   Contexte base de données
│   ├── Services/               Services métier
│   ├── DTOs/                   Data Transfer Objects
│   └── Migrations/             Migrations EF Core
│
├── Frontend/                   Application Angular
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/     Composants UI
│   │   │   ├── services/       Services Angular
│   │   │   └── models/         Interfaces TypeScript
│   │   └── assets/             Images et styles
│   └── package.json
│
└── Screenshots/                Captures d'écran du projet
    ├── 01-accueil.png
    ├── 02-detail-event.png
    ├── 03-selection-places.png
    ├── 04-reservation.png
    ├── 05-paiement.png
    ├── 06-mes-reservations.png
    └── 07-billet-pdf.png

================================================================================
INSTALLATION ET LANCEMENT
================================================================================

PRÉREQUIS
---------
- .NET 8 SDK - https://dotnet.microsoft.com/download
- Node.js 18+ - https://nodejs.org/
- SQL Server (LocalDB ou Express)
- Visual Studio 2022 (recommandé pour le backend)
- Angular CLI : npm install -g @angular/cli

--------------------------------------------------------------------------------
BACKEND (.NET API)
--------------------------------------------------------------------------------

INSTALLATION LIGNE DE COMMANDE
-------------------------------
cd Backend

Restaurer les packages NuGet (si pas dans Visual Studio)
dotnet restore

Mettre à jour la connection string dans appsettings.json
Ouvrir appsettings.json et modifier "DefaultConnection"

Créer la base de données
dotnet ef database update

Lancer l'API
dotnet run

INSTALLATION AVEC VISUAL STUDIO
--------------------------------
1. Ouvrir Backend/MisterTicket.Api.sln
2. Clic droit sur la solution → Restaurer les packages NuGet
3. Ouvrir Package Manager Console
4. Exécuter : Update-Database
5. Appuyer sur F5 pour lancer

L'API sera accessible sur : https://localhost:7255
Documentation Swagger : https://localhost:7255/swagger

--------------------------------------------------------------------------------
FRONTEND (ANGULAR)
--------------------------------------------------------------------------------

INSTALLATION
------------
cd Frontend

Installer les dépendances
npm install

Lancer le serveur de développement
ng serve

L'application sera accessible sur : http://localhost:4200

================================================================================
COMPTE DE TEST
================================================================================

Pour tester l'application, utilisez les identifiants suivants :

Email : admin@test.com
Mot de passe : Admin123!

================================================================================
CAPTURES D'ÉCRAN
================================================================================

Les captures d'écran de l'application en fonctionnement sont disponibles 
dans le dossier Screenshots/.

Elles montrent :
1. Page d'accueil avec liste des événements
2. Détail d'un événement avec carte du terrain
3. Sélection interactive des places
4. Processus de réservation
5. Confirmation de paiement
6. Page "Mes Réservations"
7. Billet PDF généré avec QR code

================================================================================
DESIGN
================================================================================

Le design s'inspire des couleurs officielles de Roland Garros :
- Orange (#E67E22) - Terre battue
- Vert foncé (#00594C) - Couleur signature
- Beige (#F5E6D3) - Élégance
- Blanc - Contraste

================================================================================
FONCTIONNALITÉS TECHNIQUES DÉTAILLÉES
================================================================================

AUTHENTIFICATION
----------------
- Inscription avec validation email
- Connexion avec token JWT
- Durée de session configurable
- Déconnexion sécurisée

RÉSERVATION
-----------
- Sélection multiple de sièges
- Vérification disponibilité en temps réel
- Réservation temporaire (15 min)
- Calcul automatique du montant total

PAIEMENT
--------
- Simulation de paiement sécurisé
- Validation des données
- Confirmation immédiate
- Mise à jour du statut

GÉNÉRATION DE BILLETS
----------------------
- PDF professionnel avec logo Roland Garros
- QR code unique par billet
- Informations complètes (événement, siège, prix)
- Téléchargement instantané

================================================================================
SÉCURITÉ
================================================================================

- Hashage des mots de passe avec BCrypt
- Tokens JWT avec expiration
- Validation des entrées côté serveur
- Protection CORS configurée
- HTTPS activé en production

================================================================================
TESTS
================================================================================

BACKEND
-------
cd Backend
dotnet test

FRONTEND
--------
cd Frontend
ng test

================================================================================
BASE DE DONNÉES
================================================================================

ENTITÉS PRINCIPALES
-------------------
- Users - Utilisateurs de la plateforme
- Events - Événements Roland Garros
- Courts - Courts de tennis
- Seats - Sièges disponibles
- Reservations - Réservations effectuées
- Payments - Paiements traités

RELATIONS
---------
- Un événement → Un court
- Un court → Plusieurs sièges
- Une réservation → Plusieurs sièges
- Une réservation → Un paiement

================================================================================
LIMITATIONS CONNUES
================================================================================

- Paiement en mode simulation uniquement
- Pas d'envoi d'emails réels (simulation console)
- Base de données locale (pas de cloud)
- Pas de gestion multi-langues

================================================================================
OBJECTIFS PÉDAGOGIQUES ATTEINTS
================================================================================

- Architecture client-serveur
- API RESTful avec .NET
- Frontend moderne avec Angular
- Base de données relationnelle
- Authentification sécurisée
- Temps réel avec SignalR
- Génération de documents
- Design responsive


================================================================================
                              FIN DU DOCUMENT
================================================================================