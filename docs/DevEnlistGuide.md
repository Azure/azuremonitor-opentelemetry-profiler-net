# Developer Enlistment

Because of the special settings of the repo, combing symbolic links and submodules, there are some special sequence needed to get the repository ready for contribution. Here's the guidance.

## Pre-requisition

* PowerShell 7 (if you want to leverage the powershell scripts.)
* Git Credential Manager (to access devdiv repo)

## Enlist

1. Have your own fork of this repository on GitHub.

1. Assuming you are on Windows, clone the repo, and enable symbolic links

    ```shell
    git clone https://github.com/your-github-handle/azuremonitor-opentelemetry-profiler-net.git
    cd azuremonitor-opentelemetry-profiler-net
    git config core.symlinks true
    ```

1. Pull down the submodules of `ServiceProfiler`

    ```shell
    git submodule update --init --progress
    ```

1. Open another command prompt **as administrator**, and reset the repro:

    ```shell
    git reset --hard
    exit
    ```

If you got everything correct, now you can run the build script to build the code successfully:

```powershell
.\src\ServiceProfiler.EventPipe.Otel\tools\BuildSolution.ps1 Debug
```
