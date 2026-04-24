# EasySave v1.0 - Backup Management System

**EasySave** is a professional backup software solution developed by **ProSoft**. This version (1.0) is a robust console-based application designed to manage up to 5 backup jobs with real-time logging and precise state tracking.

---

## 📋 Features

* **Backup Types:** Full and Differential backup support.
* **Multilingual Interface:** Fully functional in **English** and **French**.
* **Job Management:** Create, save, and manage up to 5 unique backup configurations.
* **Execution Modes:**
    * **Interactive Menu:** Step-by-step selection for easy use.
    * **Sequential Execution:** Run all configured jobs in one go.
    * **Command Line Interface (CLI):** Support for specific job ranges (e.g., `1-3`) or selections (e.g., `1;3`).
* **Real-time Logging:** Every file transfer is recorded daily via the `EasyLog.dll` library.
* **State Tracking:** A dedicated `state.json` file monitors progress, file sizes, and current status in real-time.

---

## 🛠 Technical Specifications

* **Framework:** .NET 8.0
* **Language:** C#
* **Architecture:**
    * **Singleton Pattern:** Implemented in `LoggerService` to ensure thread-safe logging.
    * **Modular Design:** Clean separation between Models, Views, Controllers (MVC), and Services.
    * **External Library:** All logging logic is encapsulated in `EasyLog.dll` for portability and reusability.
* **Storage:** * Logs and state files are stored in a dedicated `/Logs` directory (standardized for client servers).
    * All data files use the **JSON** format with indented formatting for human readability.

---

## 🚀 Installation & Running

### Prerequisites
* Visual Studio 2022 or higher.
* .NET 8.0 SDK.

### Build
1. Open the `EasySave.sln` solution in Visual Studio.
2. Build the solution (Build > Build Solution).

### Run via Console
```bash
# To launch the interactive menu
./EasySave.exe

# To run jobs 1 to 3 automatically
./EasySave.exe 1-3

# To run jobs 1 and 3 specifically
./EasySave.exe 1;3
