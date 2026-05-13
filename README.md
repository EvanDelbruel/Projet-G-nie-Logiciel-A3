# EasySave v3.0 - Backup Management System

**EasySave** is a professional data backup software solution published by **ProSoft**. This 3.0 version marks a major evolution by introducing parallel data processing, fine-grained network priority management, and centralized activity logging via Docker.

---

## Key Features

### Backup Management
* **Unlimited Jobs**: Create and configure an unlimited number of backup jobs.
* **Backup Types**: Support for both full and differential backups.
* **Parallel Execution**: Transition from sequential processing to simultaneous job execution using an asynchronous architecture (Task Parallel Library).

### Real-Time Control & Interaction
* **Graphical Interface (WPF)**: Modern GUI based on the MVVM architecture allowing visual progress tracking.
* **Individual Control**: Ability to pause, resume, or stop each job independently using dedicated task controllers.
* **Progress Indicators**: Real-time calculation of the completion percentage, remaining file count, and total processed size, updated seamlessly via the WPF Dispatcher.

### Optimization & Security
* **Priority Management**: Transfer prioritization based on user-defined file extensions to guarantee immediate processing of critical data.
* **Large File Limitation**: Semaphore-based locking system preventing simultaneous transfers of multiple files exceeding a configurable size threshold (in KB) to preserve bandwidth.
* **Business Software Detection**: Automatic suspension and transparent resumption of transfers if specific business software is detected running.
* **CryptoSoft Encryption**: Integration of an external XOR encryption module, protected by a system Mutex to guarantee strictly controlled mono-instance execution.

### Logging & Centralization
* **Multi-Format**: Export daily logs in JSON or XML format based on chosen configuration.
* **Docker Centralization**: TCP network service allowing logs to be sent to a centralized server (Docker) with user and source machine identification for full traceability.

---

## Technical Architecture

The project is developed in **C#** under the **.NET 8.0** framework and strictly follows **Software Engineering** principles.

### Design Patterns Used
* **MVVM (Model-View-ViewModel)**: Strict separation between business logic, data models, and user interface.
* **Singleton**: Applied to the `LoggerService` to guarantee a single, thread-safe access point for log writing operations.
* **Command Pattern**: Usage of `RelayCommand` to bind UI actions to ViewModel methods.

### Concurrency Management
* **SemaphoreSlim**: Used in the `SyncManager` to limit simultaneous access to CryptoSoft and restrict the transfer of large files.
* **ManualResetEvent**: Implementation of synchronization barriers for the thread pause/resume system.
* **CancellationToken**: Management of safe background task cancellation when a user stops a job.

---

## Project Structure

* **EasySaveWPF.Models**: Definition of data structures (`BackupJob`, `JobController`, `LogModel`, `StateModel`).
* **EasySaveWPF.ViewModels**: Presentation logic and user command management via `MainViewModel`.
* **EasySaveWPF.Services**: Backup orchestration (`BackupService`) and global synchronization management (`SyncManager`).
* **EasyLog.dll**: Shared library encapsulating local and remote logging logic.
* **CryptoSoft**: External security module with mutual exclusion management via OS Mutex.
* **LogServer**: Asynchronous TCP server for receiving and aggregating centralized logs (Docker).

---

## Installation & Configuration

### Prerequisites
* Microsoft Visual Studio 2022.
* .NET 8.0 SDK or higher.
* Docker (for deploying the centralized log server).

### Configuration (`settings.json`)
The application is driven by a configuration file allowing you to define the following parameters:
* `BusinessSoftware`: Process name triggering automatic pause.
* `CryptoExtensions`: List of file extensions to be processed by CryptoSoft.
* `PriorityExtensions`: Priority extensions to be processed at the beginning of the queue.
* `MaxFileSizeKb`: Size threshold (in KB) for simultaneous transfer restriction.
* `LogFormat`: Output format (`JSON` or `XML`).
* `LogDestination`: Stream destination (`Local`, `Docker`, or `Both`).

---

## Usage

### Graphical Mode
Launch `EasySaveWPF.exe`. The main interface allows you to manage the job list and control executions. General settings are accessible via the dedicated configuration button.

### Command Line Interface (CLI) Mode
The software supports direct execution via launch arguments:
* `EasySaveWPF.exe 1-3`: Automatic execution of jobs 1 to 3.
* `EasySaveWPF.exe 1;3;5`: Selective execution of the indicated jobs.

---

## Future Developments (V4.0)

* Performance benchmarking between parallel and sequential backups according to storage types (SSD vs HDD).
* Implementation of native compression algorithm prior to transfer.
* Integration of remote transfer protocols (SFTP/Cloud Storage).
* Advanced asymmetric encryption for sensitive data protection.

---

## License

All rights reserved. Developed by the ProSoft project team.

*Note: ProSoft is a fictitious company used for educational purposes.*
