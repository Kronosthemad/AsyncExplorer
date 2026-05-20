# AsyncExplorer

AsyncExplorer is a lightweight, multi-threaded file explorer built with C# and Windows Forms on .NET 10.0. It is designed for fast filesystem navigation with a minimal footprint.

## Project Overview

- **Main Technologies:** C#, .NET 10.0-windows, WinForms.
- **Key Features:**
  - Asynchronous directory scanning using `Task.Run` and `IProgress<T>`.
  - Simple, responsive UI for rapid file/folder exploration.
  - Multi-threaded operations to prevent UI freezing during large directory scans.
  - Configurable settings via the Windows Registry.
  - Dual error logging: Local file-based (`error.log`) or Windows Event Viewer.

## Architecture

The project follows a modular structure to separate UI concerns from filesystem operations:

- **`Program.cs`**: Entry point that initializes the application and sets up global exception handling.
- **`MyForms/`**: Contains the UI definitions.
  - `AsyncDirectoryForm.cs`: The main application window, manually constructing the layout and handling user interactions.
  - `SettingsForm.cs`: Provides a UI for configuring application preferences.
- **`Services/`**:
  - `AsyncDirectoryController.cs`: The core logic for filesystem traversal. It manages navigation state and uses progress reporting to update the UI asynchronously.
  - `AppSettings.cs`: Manages application settings (e.g., showing hidden files, logging preference) using the Windows Registry (`Software\Kronos\AsyncExplorer`).
- **`Model/`**:
  - `FileItem.cs`: A data model representing a file or directory in the explorer.

## Building and Running

### Prerequisites
- .NET 10.0 SDK (Windows-only)

### Commands
- **Build:** `dotnet build`
- **Run:** `dotnet run`
- **Clean:** `dotnet clean`

> [!NOTE]
> This application is specifically targeted at Windows platforms as it relies on `net10.0-windows`, WinForms, and the Windows Registry.

## Development Conventions

- **Code Style:**
  - Uses **Allman style** (braces on new lines) for classes, methods, and control blocks.
  - Private fields are prefixed with an underscore (e.g., `_controller`).
  - Indentation uses tabs.
- **Asynchronous Patterns:**
  - UI-bound long-running operations must be performed using `Task.Run` to maintain responsiveness.
  - UI updates from background threads must be handled via `IProgress<T>` or `Control.Invoke`/`Control.BeginInvoke`.
- **Error Handling:**
  - Errors should be logged using `AsyncDirectoryForm.LogError(Exception)`.
  - Logging behavior is controlled by the `UseEventViewer` setting.
- **Settings:**
  - Global settings should be added to `AppSettings.cs` and persisted in the registry.
