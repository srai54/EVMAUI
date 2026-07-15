# Superpowers Development Workflow

This project uses the Superpowers agentic skills framework. All skills are available as `/` commands (e.g., `/brainstorming`, `/test-driven-development`).

## Core Workflow

1. **Before any code** - Use `/brainstorming` to refine ideas into specs
2. **Before implementation** - Use `/writing-plans` to create bite-sized tasks
3. **During implementation** - Use `/test-driven-development` (RED-GREEN-REFACTOR)
4. **When debugging** - Use `/systematic-debugging` (4-phase root cause)
5. **Before completion** - Use `/verification-before-completion` (evidence before claims)
6. **When merging** - Use `/requesting-code-review` then `/finishing-a-development-branch`

## Available Commands

| Command | Purpose |
|---------|---------|
| `/brainstorming` | Design refinement before code |
| `/writing-plans` | Create implementation plans |
| `/test-driven-development` | RED-GREEN-REFACTOR cycle |
| `/systematic-debugging` | 4-phase root cause debugging |
| `/subagent-driven-development` | Parallel subagent execution |
| `/executing-plans` | Batch plan execution |
| `/dispatching-parallel-agents` | Dispatch independent tasks |
| `/requesting-code-review` | Pre-merge review |
| `/receiving-code-review` | Process review feedback |
| `/using-git-worktrees` | Isolated workspace setup |
| `/verification-before-completion` | Verify before claiming done |
| `/finishing-a-development-branch` | Merge/PR/cleanup decisions |
| `/writing-skills` | Create new superpowers skills |

## Tool Mapping

When skills describe actions, use OpenCode equivalents:
- `todowrite` for task tracking
- `task` with `subagent_type: "general"` for subagents
- `task` with `subagent_type: "explore"` for codebase exploration
- `skill` tool to load skills
- `read`, `write`, `edit` for file operations
- `bash` for shell commands
- `grep`, `glob` for searching
- `webfetch` for URL fetching
