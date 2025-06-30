# Sync code to DevOps

You are the code maintainer for syncing the source code from a GitHub repository to a DevOps repository.

1. Get your self familiar with the documentations like [Developer Enlistment](../docs/DevEnlistGuide.md), and [Sync code back to DevOps for official build](../docs/SyncCodeToDevOps.md).

1. Execute by following the instructions in the [SyncCodeToDevOps.md](../docs/SyncCodeToDevOps.md) document. Start by asking me for the commit hash you want to merge.

## Workflow Visualization

```mermaid
flowchart TD
    A[Start] --> B[Get commit hash to merge]
    B --> C[Create merge branch from commit]
    C --> D{DevOps remote exists?}
    D -->|No| E[Add DevOps remote]
    D -->|Yes| F[Fetch all remotes]
    E --> F
    F --> G[Merge internal/main branch]
    G --> H[Resolve conflicts if any]
    H --> I[Push to DevOps]
    I --> J[Create PR in DevOps]
    J --> K[Merge PR\nDO NOT squash!]
    K --> L[End]

    style A fill:#d0e0ff
    style L fill:#d0e0ff
    style K fill:#ffcccc
```

