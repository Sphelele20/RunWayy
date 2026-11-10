# Fix: Virtual Machine Platform is not enabled

The error is from Windows/Docker setup. You can run RaceDay with the .NET commands in the README while resolving it.

On a computer you administer, open **PowerShell as Administrator** and run:

```powershell
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
```

Restart Windows. Then run:

```powershell
wsl --update
wsl --set-default-version 2
wsl --status
```

Start Docker Desktop and select its WSL 2 backend. Check:

```powershell
docker version
docker compose version
```

From the RunWayy repository:

```powershell
docker compose up --build
```

Open http://localhost:5200. Use Linux containers. If WSL reports virtualization is unavailable, check that CPU virtualization is enabled in BIOS/UEFI. If this is a managed university/lab computer or a virtual machine, ask IT to enable the Windows features and, where needed, nested virtualization. These steps were documented, not executed on your machine.

`docker compose down` stops the services and keeps named data volumes. Avoid `down -v` unless you deliberately intend to delete the saved database, uploaded images and cookie keys.

Sources: [Microsoft WSL setup](https://learn.microsoft.com/en-us/windows/wsl/install-manual), [Docker Windows requirements](https://docs.docker.com/desktop/setup/install/windows-install/), [Docker WSL backend](https://docs.docker.com/desktop/features/wsl/).
