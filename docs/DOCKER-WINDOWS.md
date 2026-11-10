# Virtual Machine Platform error

On a computer you administer, open PowerShell **as Administrator**:

```powershell
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
```

Restart Windows, then run:

```powershell
wsl --update
wsl --set-default-version 2
wsl --status
```

Start Docker Desktop with its WSL 2 backend, then run docker compose up --build in the project folder. CPU virtualization must be enabled in BIOS/UEFI. On a managed lab PC or VM, university IT may need to enable the features/nested virtualization. No Windows settings were changed automatically.

You can run the application without Docker using START-HERE.md while this is resolved.

Source: [Microsoft WSL instructions](https://learn.microsoft.com/en-us/windows/wsl/install-manual).
