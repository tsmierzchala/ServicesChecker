# ServicesChecker

![Build Status](https://github.com/tsmierzchala/ServicesChecker/actions/workflows/dotnet-desktop.yml/badge.svg)

A Windows desktop application for monitoring and managing Windows services, Docker containers, and log files. Built with .NET 9 and WPF.

## Overview

ServicesChecker is a utility tool designed to simplify the management of local Windows services and REST services, Docker containers, and log files monitoring in a development or IT operations environment. The application provides a simple, user-friendly interface to track service status, manage Docker containers, and monitor log files.

## Features

### Service Management
- Monitor both local Windows services and REST API services
- Add, delete, and track service status with automatic refresh
- Start, stop, and restart local Windows services
- Visual status indicators showing service health
- Flag services that connect to databases for coordinated management with Docker

### Docker Container Management
- Manage Docker containers directly from the application
- Select which container to run while automatically stopping others
- Smart shutdown of database-dependent services before container switching
- Configuration-based container filtering

### Log File Monitoring
- Track log files from various locations
- Visual indicators for file existence and status
- Display file sizes with automatic updates
- Easily delete log files or open their location in File Explorer

### Application Management
- Ability to download and manage application packages
- Temporary storage and extraction of application archives

## System Requirements

- Windows 10/11 or Windows Server 2016 or newer
- .NET 9.0 Runtime
- Docker Desktop (for Docker container features)

## Installation

### Download and Install

1. Download the latest release from the [Releases](https://github.com/tsmierzchala/ServicesChecker/releases) page
2. Extract the ZIP archive to your preferred location
3. Run `ServicesChecker.exe`

No installation required - the application is self-contained.

### Build from Source

## Usage

### Managing Services

1. Enter a service name in the text box (either a Windows service name or a URL for REST services)
2. Check "Is Connecting to DB" if the service connects to a database
3. Click "Add Service" to begin monitoring
4. Right-click on services in the list to:
   - Change status (start/stop services)
   - Restart services
   - Delete services from the monitoring list

### Working with Docker Containers

1. Select a container from the dropdown to activate it
2. The application will automatically:
   - Stop any database-dependent services
   - Stop other running containers
   - Start the selected container

### Managing Log Files

1. In the Configuration tab, click "Browse" to select log files to monitor
2. The application will display the status and size of each log file
3. Right-click on files to:
   - Delete individual files
   - Open the file location in Explorer
4. Use "Delete From Disk" to clean up all log files while keeping them in the monitoring list

## Configuration

The application uses two JSON configuration files:

### config.json

Contains Docker container names to be managed:

### services.json and logfiles.json

Automatically created to store your services and log files configuration.

## Build and Deployment

The project uses GitHub Actions for CI/CD:

- Automatic build on push to main branch or pull requests
- Creation of self-contained single file executable for Windows
- Automatic version numbering based on date and build number
- Creation of GitHub releases with downloadable artifacts

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
