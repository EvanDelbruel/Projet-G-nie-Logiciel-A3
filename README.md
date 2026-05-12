# EasySave v3.0 - Backup Management System

**EasySave** est une solution logicielle professionnelle de sauvegarde de données éditée par **ProSoft**. Cette version 3.0 marque une évolution majeure en introduisant le traitement parallèle des données, une gestion fine des priorités réseau et une centralisation des journaux d'activité via Docker.

---

## Fonctionnalités principales

### Gestion des sauvegardes
* **Travaux illimités** : Création et configuration d'un nombre illimité de travaux de sauvegarde.
* **Types de sauvegarde** : Support des sauvegardes complètes et différentielles.
* **Sauvegarde en parallèle** : Abandon du mode séquentiel pour une exécution simultanée des travaux via une architecture asynchrone (Task Parallel Library).

### Contrôle et interaction en temps réel
* **Interface Graphique (WPF)** : IHM moderne basée sur l'architecture MVVM permettant un suivi visuel de la progression.
* **Contrôle individuel** : Possibilité de mettre en pause, de reprendre ou d'arrêter chaque travail indépendamment via des contrôleurs de tâches dédiés.
* **Indicateurs de progression** : Calcul en temps réel du pourcentage d'avancement, du nombre de fichiers restants et de la taille totale traitée, mis à jour via le Dispatcher WPF.

### Optimisation et sécurité
* **Gestion des priorités** : Priorisation des transferts selon les extensions de fichiers définies par l'utilisateur pour garantir le traitement immédiat des données critiques.
* **Limitation des fichiers volumineux** : Système de verrouillage par sémaphore empêchant le transfert simultané de plusieurs fichiers dépassant un seuil de taille configurable (n Ko) afin de préserver la bande passante.
* **Détection du logiciel métier** : Suspension automatique et reprise transparente des transferts si un logiciel spécifique est détecté en cours d'exécution.
* **Cryptage CryptoSoft** : Intégration du module de cryptage XOR externe, protégé par un Mutex système pour garantir une exécution mono-instance strictement contrôlée.

### Journalisation et Centralisation
* **Multi-format** : Exportation des logs journaliers au format JSON ou XML selon la configuration choisie.
* **Centralisation Docker** : Service réseau TCP permettant l'envoi des logs vers un serveur centralisé (Docker) avec identification de l'utilisateur et de la machine source pour une traçabilité complète.

---

## Architecture technique

Le projet est développé en **C#** sous le framework **.NET 8.0** et suit rigoureusement les principes du **Génie Logiciel**.

### Design Patterns utilisés
* **MVVM (Model-View-ViewModel)** : Séparation stricte entre la logique métier, les modèles de données et l'interface utilisateur.
* **Singleton** : Appliqué au `LoggerService` pour garantir un point d'accès unique et thread-safe aux opérations d'écriture de journaux.
* **Command Pattern** : Utilisation de `RelayCommand` pour lier les actions de l'IHM aux méthodes du ViewModel.

### Gestion des accès concurrentiels
* **SemaphoreSlim** : Utilisé dans le `SyncManager` pour limiter les accès simultanés à CryptoSoft et restreindre le transfert de fichiers volumineux.
* **ManualResetEvent** : Implémentation de la barrière de synchronisation pour le système de pause/reprise des threads.
* **CancellationToken** : Gestion de l'annulation sécurisée des tâches de fond lors de l'arrêt d'un travail par l'utilisateur.

---

## Structure du projet

* **EasySaveWPF.Models** : Définition des structures de données (`BackupJob`, `JobController`, `LogModel`, `StateModel`).
* **EasySaveWPF.ViewModels** : Logique de présentation et gestion des commandes utilisateur via `MainViewModel`.
* **EasySaveWPF.Services** : Orchestration des sauvegardes (`BackupService`) et gestion globale de la synchronisation (`SyncManager`).
* **EasyLog.dll** : Bibliothèque partagée encapsulant la logique de journalisation locale et distante.
* **CryptoSoft** : Module externe de sécurité avec gestion de l'exclusion mutuelle par Mutex.
* **LogServer** : Serveur TCP asynchrone de réception et d'agrégation des logs centralisés.

---

## Installation et configuration

### Prérequis
* Microsoft Visual Studio 2022.
* SDK .NET 8.0 ou supérieur.
* Docker (pour le déploiement du serveur de logs centralisé).

### Configuration (`settings.json`)
L'application est pilotée par un fichier de configuration permettant de définir les paramètres suivants :
* `BusinessSoftware` : Nom du processus déclenchant la mise en pause automatique.
* `CryptoExtensions` : Liste des extensions de fichiers à traiter par CryptoSoft.
* `PriorityExtensions` : Extensions prioritaires à traiter en début de file d'attente.
* `MaxFileSizeKb` : Seuil de taille (en Ko) pour la restriction de transfert simultané.
* `LogFormat` : Format de sortie (`JSON` ou `XML`).
* `LogDestination` : Destination des flux (`Local`, `Docker`, ou `Both`).

---

## Utilisation

### Mode Graphique
Lancez `EasySaveWPF.exe`. L'interface principale permet de gérer la liste des travaux et de piloter les exécutions. Les paramètres généraux sont accessibles via le bouton de configuration dédié.

### Mode Ligne de Commande (CLI)
Le logiciel prend en charge l'exécution directe via des arguments de lancement :
* `EasySave.exe 1-3` : Exécution automatique des travaux 1 à 3.
* `EasySave.exe 1;3;5` : Exécution sélective des travaux indiqués.

---

## Évolutions futures (V4.0)

* Analyse comparative de performance entre sauvegardes parallèles et séquentielles selon les types de supports (SSD vs HDD).
* Implémentation d'un algorithme de compression native avant transfert.
* Intégration de protocoles de transfert distants (SFTP/Cloud Storage).
* Chiffrement asymétrique avancé pour la protection des données sensibles.

---

## Licence

Tous droits réservés. Développé par l'équipe projet ProSoft.

*Note : ProSoft est une entreprise fictive utilisée dans un cadre pédagogique.*
