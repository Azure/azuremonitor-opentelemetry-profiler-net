# Sync code back to DevOps for official build

This is for internal use only.

1. Follow the steps in [DevEnlistGuide](./DevEnlistGuide.md) to have a local enlistment of the repository of the official github repo.

   _Notes: **make sure** your are on the commit you intend to merge._

1. Create a branch for merging, for example

    ```shell
    git checkout -b merge-9cdab49 9cdab49   # Check out branch merge-9cdab49 off commit 9cdab49
    git log -1 --format=%H                  # Double check this IS the commit you want to merge
    ```

1. Add remote of DevOps repo if you haven't. For example:

    ```shell
    git remote add internal <Url>           # Get the URL from the DevOps UI.
    ```

1. Fetch everything

    ```shell
    git fetch -p --all                      # Making sure the local repository and the remote are in sync.
    ```

1. Do a forward integration

    ```shell
    git merge internal/main                 # This is the chance to resolve conflict, if any is expected.
    ```

    No conflict is really expected here. If there is, please execute with cautious.

1. Push the merged content

    ```shell
    git push internal HEAD                  # Push the branch to remote of internal, ready for PR.
    ```

    You will see a message telling you that the branch is ready to be merged on the DevOps.

1. Use that URL to submit a PR for merging.

    _Notes: do **NOT** use `squash & merge` to avoid introducing conflicts in the future. That should be disabled. But in case it is somehow allowed, do NOT do it._
