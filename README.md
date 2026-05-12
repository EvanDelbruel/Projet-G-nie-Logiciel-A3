# 💾 EasySave - V2.0 (WPF GUI & Encryption)

EasySave V2.0 is a major milestone for the ProSoft backup utility. This release completely abandons the legacy console interface in favor of a modern, intuitive Graphical User Interface (GUI) built with WPF. It also introduces critical enterprise features such as file encryption and business software detection.

## 🚀 What's New in V2.0

* **Modern Graphical Interface (WPF):** A fluid, user-friendly UI built with Windows Presentation Foundation. The interface is fully bilingual (English and French) and updates dynamically.
* **Unlimited Backup Jobs:** The previous limit of 5 jobs has been removed. Users can now create, manage, and execute an unlimited number of backup configurations.
* **File Encryption (`CryptoSoft`):** Integration of an external executable (`CryptoSoft.exe`) to perform XOR encryption on specific file extensions defined by the user in the settings.
* **Business Software Detection:** EasySave now monitors running processes. If a specified business software (e.g., `notepad`) is detected, backup executions are forbidden or safely interrupted to prioritize enterprise resources.
* **Enhanced Logging:** Daily logs now track the exact time taken to encrypt files (`FileEncryptTime`). The system logs >0ms for successful encryption, 0 for standard copies, and -1 if an error occurs.

## ✨ Core Features (Maintained & Upgraded)

* **Backup Types:** Supports both **Full** and **Differential** backups.
* **Dynamic Log Formatting:** Users can seamlessly choose between **JSON** and **XML** formats for their daily logs.
* **Real-Time Tracking:** The `state.json` file is updated continuously, allowing the UI to display the exact progress of the active backup task.

## 📐 Software Architecture

* **MVVM Design Pattern:** The application strictly adheres to the Model-View-ViewModel (MVVM) architecture, ensuring a total decoupling of the UI (XAML) from the business logic.
* **Command Routing:** User interactions are cleanly managed through the `ICommand` interface via custom Relay Commands.
* **Thread-Safe Singleton:** The `LoggerService` continues to use a locked Singleton pattern to prevent file access conflicts between the UI and the backup engine.

## 💻 Installation & Usage

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) installed on your machine.
* Visual Studio 2022 (recommended for execution and compilation).

### Launching the Application
1. Open the solution file (`EasySave.sln`) in Visual Studio.
2. Set the `EasySaveWPF` project as the startup project.
3. Run the application (F5).
4. **Important:** Ensure the `CryptoSoft.exe` tool is located in the `CryptoSoftTool` folder relative to the execution path to enable encryption features.