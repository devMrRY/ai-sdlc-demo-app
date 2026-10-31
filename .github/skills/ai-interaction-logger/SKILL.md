---
name: ai-interaction-logger
description: Logs prompts, tool actions, tool results, session lifecycle events, and raw hook payloads for audit and debugging.
---

# AI interaction logger

- Capture the user prompt with a timestamp and session id.
- Record each tool action with its name, input, result, and any error.
- Group events by session and prompt so all activity for one request stays together.
- Persist raw hook payloads for debugging when available.
- Record lifecycle events such as prompt submission, tool pre/post execution, and stop/end events.
- Store logs as JSON Lines in `.github/logs/ai-interactions.jsonl` and grouped prompt/session files under `.github/logs/sessions/`.
- Do not duplicate the same prompt entry within a single session; keep one prompt summary per prompt group.
- When a tool fails, record the failure detail and still keep the tool name and attempted action.
- Keep output machine-readable and easy to inspect in a text editor or log viewer.