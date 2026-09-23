
![C#](https://img.shields.io/badge/C%23-13.0-512BD4?style=for-the-badge&logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?style=for-the-badge&logo=sqlite&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10%2B-0078D4?style=for-the-badge&logo=windows11&logoColor=white)

# WorkTime

WorkTime is a lightweight desktop time-tracking application for Windows, built with C# and WPF.

It is designed for personal project-based work tracking: select a project, start the timer, pause when needed, and let WorkTime calculate worked time and financial statistics automatically.

The application works completely locally and does not require an account, server, API, or internet connection.

## Features

- Project-based time tracking
- Start / Pause / Resume / Finish workflow
- Multiple work sessions during the same day
- Automatic recovery of an unfinished running or paused session after restarting the application
- Switching between projects while the timer is running
- Correct time calculation across midnight and multiple calendar days
- Daily worked-time calculation
- Work history by Day / Week / Month
- Project hourly rate
- Optional agreed project price
- Automatic earnings calculation
- Remaining amount calculation
- Project progress calculation
- Effective hourly rate calculation
- Configurable daily work target
- Configurable reminder before reaching the daily target
- Persistent desktop notifications
- System tray support
- Local project settings
- Permanent project deletion together with its work history
- Local SQLite database
- No registration and no cloud dependency

## Timer Behavior

WorkTime uses real work sessions stored in the database rather than continuously saving a counter.

A calendar day is calculated from `00:00` to `24:00`.

You can finish work and start again later on the same day. All work sessions are combined when daily statistics are calculated.

Pauses are not counted as worked time.

If a session crosses midnight, WorkTime automatically attributes the correct amount of time to each calendar day.

When switching projects while the timer is running, the previous project session ends and the new project session starts at exactly the same timestamp.

Closing WorkTime does not destroy an active timer state. Running and paused states are persisted and restored the next time the application starts.

## Project Analytics

For each project WorkTime can calculate:

- Total worked time
- Current hourly rate
- Earned amount
- Agreed project price
- Remaining amount
- Project progress
- Effective hourly rate

Financial values are calculated dynamically from the recorded work time and the project's current hourly rate.

Changing the hourly rate therefore recalculates historical financial analytics without modifying the recorded work sessions.

## Daily Target

Each project can optionally have:

- Daily target in minutes
- Reminder time before reaching the target
- Notifications enabled or disabled

WorkTime can notify you shortly before reaching the configured daily target and again when the target has been reached.

## Data Storage

All application data is stored locally using SQLite.

The database is located in:

```text
%LOCALAPPDATA%\WorkTime\worktime.db
```

The database is not stored inside the application directory or Git repository.

## Technology

- C#
- .NET 10
- WPF
- Entity Framework Core
- SQLite
- xUnit
- Windows Forms `NotifyIcon` for system tray integration

## Project Structure

```text
WorkTime/
├── Data/
├── Models/
├── Services/
├── ViewModels/
├── Views/
└── Assets/
```

## Build

Requirements:

- Windows 10 or newer
- .NET 10 SDK
- Visual Studio with .NET desktop development support

```powershell
dotnet build .\WorkTime.slnx
```

Run tests:

```powershell
dotnet test .\WorkTime.slnx
```

## Publish

Create a self-contained Windows x64 release:

```powershell
dotnet publish .\WorkTime\WorkTime.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
```

The published application is placed in the `publish` directory.

Run `WorkTime.exe`.

User data remains in `%LOCALAPPDATA%\WorkTime` and is independent of the published application files.

## Privacy

WorkTime is a local desktop application. It does not require user accounts, authentication, cloud storage, external APIs, or internet access.

Work-time and project data remain on the local computer.

## License

This project is currently provided without a separate open-source license.

Copyright © 2026 Aleksandr Stanov
