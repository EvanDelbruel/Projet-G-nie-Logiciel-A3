# 💾 EasySave - V1.1 (Console Application)

EasySave is a file backup utility software developed in C# (.NET 8.0) for ProSoft. This V1.1 release is an upgraded version of the initial command-line interface (CLI) application, introducing new data formatting capabilities while maintaining a lightweight footprint.

## 🚀 What's New in V1.1 (Compared to V1.0)

This minor release focuses on fulfilling specific enterprise client requirements regarding data logging:

* **Dynamic Log Formatting (JSON & XML):** Unlike V1.0 which was restricted to JSON, users can now dynamically choose their preferred daily log format (JSON or XML) directly from the application's settings menu.
* **Persistent User Settings:** The application now generates a `settings.json` file to securely save the user's log format preference between sessions.
* **Upgraded `EasyLog.dll`:** The independent logging library has been updated to handle XML serialization securely, without breaking backward compatibility with V1.0's architecture.

## ✨ Core Features (Maintained from V1.0)

* **Job Management:** Create, display, and execute up to 5 customizable backup jobs.
* **Backup Types:** Supports **Full** backups (complete copy) and **Differential** backups (copies only files modified since the last backup).
* **Bilingual Interface:** The interactive console menu is accessible in both English and French.
* **Real-Time Tracking:** Dynamically updates a `state.json` file to monitor the exact progress, remaining files, and current state of ongoing backups in real-time.
* **CLI Automation:** Users can bypass the interactive menu and run specific jobs directly from the terminal (e.g., running `EasySave.exe 1-3` or `EasySave.exe 1;3`).

## 📐 Software Architecture

* **Strict Separation of Concerns:** The application relies on a clear Model-View-Controller (MVC) pattern adapted for console applications.
* **Singleton Pattern:** The `LoggerService` utilizes a Thread-Safe Singleton pattern to prevent file access conflicts during daily log and state file generation.

## 💻 Installation & Usage

### Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) installed on your machine.

### Launching the Interactive Menu

Navigate to the root directory of the project and run:

```bash
dotnet run --project EasySave
```

### Quick Launch (Argument Mode)

To automate execution without the visual interface, pass the job numbers as arguments:

```bash
# Execute a range (jobs 1, 2, and 3):
dotnet run --project EasySave 1-3

# Execute specific jobs (jobs 1 and 3):
dotnet run --project EasySave 1;3

# Execute a single job (job 2):
dotnet run --project EasySave 2
```