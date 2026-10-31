import fs from "node:fs";
import path from "node:path";

const hookArg = process.argv[2] || "unknown";

function readPayload() {
  try {
    const raw = fs.readFileSync(0, "utf8").trim();
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

function readJson(filePath, fallback = {}) {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf8"));
  } catch {
    return fallback;
  }
}

function writeJson(filePath, value) {
  fs.writeFileSync(filePath, JSON.stringify(value, null, 2), "utf8");
}

function appendJsonl(filePath, value) {
  fs.appendFileSync(filePath, `${JSON.stringify(value)}\n`, "utf8");
}

function readEvents(filePath) {
  try {
    const text = fs.readFileSync(filePath, "utf8");
    return text
      .split(/\r?\n/)
      .filter(Boolean)
      .map((line) => {
        try {
          return JSON.parse(line);
        } catch {
          return null;
        }
      })
      .filter(Boolean);
  } catch {
    return [];
  }
}

function appendEvent(filePath, event) {
  fs.appendFileSync(filePath, `${JSON.stringify(event)}\n`, "utf8");
}

function cleanSession(sessionPath) {
  if (!fs.existsSync(sessionPath)) {
    return;
  }

  fs.rmSync(sessionPath, { recursive: true, force: true });
}

const payload = readPayload();
const phase =
  payload.hook_event_name || payload.hookEventName || payload.phase || hookArg;

const sessionId = String(
  payload.session_id || payload.sessionId || "session-unknown",
);

const safeSessionId = sessionId.replace(/[^a-zA-Z0-9._-]/g, "_");
const logDir = path.resolve(process.cwd(), ".github", "logs");
const sessionsDir = path.join(logDir, "sessions");
const sessionDir = path.join(sessionsDir, safeSessionId);
const eventsFile = path.join(sessionDir, "events.jsonl");
const stateFile = path.join(sessionDir, "state.json");
const aggregateFile = path.join(logDir, "ai-interactions.jsonl");

fs.mkdirSync(logDir, { recursive: true });
fs.mkdirSync(sessionDir, { recursive: true });

let state = readJson(stateFile, {});

if (phase === "UserPromptSubmit") {
  fs.writeFileSync(eventsFile, "", "utf8");

  state = {
    sessionId,
    prompt:
      payload.prompt ?? payload.prompt_text ?? payload.user_prompt ?? null,
    startedAt: new Date().toISOString(),
  };

  writeJson(stateFile, state);
}

const event = {
  timestamp: new Date().toISOString(),
  phase,
  sessionId,
  prompt:
    payload.prompt ??
    payload.prompt_text ??
    payload.user_prompt ??
    state.prompt ??
    null,
  tool: payload.tool_name ?? payload.toolName ?? null,
  toolArgs: payload.tool_input ?? payload.toolArgs ?? payload.tool_args ?? null,
  toolResult:
    payload.tool_response ?? payload.toolResult ?? payload.tool_result ?? null,
  error: payload.error ?? null,
};

appendEvent(eventsFile, event);

const cleanToolArgs = (entry) =>
  entry.toolArgs && typeof entry.toolArgs === "object"
    ? (() => {
        const { code, ...rest } = entry.toolArgs;
        return rest;
      })()
    : null;

if (phase === "Stop" || phase === "SessionEnd") {
  const events = readEvents(eventsFile);
  const prompt =
    state.prompt ?? events.find((entry) => entry.prompt)?.prompt ?? null;

  const toolEvents = events.filter(
    (entry) =>
      entry.tool ||
      entry.toolArgs != null ||
      entry.toolResult != null ||
      entry.phase === "PostToolUseFailure",
  );

  const tools = [
    ...new Set(toolEvents.map((entry) => entry.tool).filter(Boolean)),
  ];
  const actionTook = toolEvents.map((entry) => ({
    tool: entry.tool ?? null,
    input: cleanToolArgs(entry),
    result: entry.toolResult ?? null,
    error: entry.error ?? null,
  }));

  const errors = toolEvents
    .filter((entry) => entry.error || entry.phase === "PostToolUseFailure")
    .map((entry) => ({
      phase: entry.phase,
      tool: entry.tool ?? null,
      error: entry.error || "Tool execution failed",
    }));

  const toolResult = toolEvents
    .filter((entry) => entry.toolResult != null)
    .map((entry) => ({
      tool: entry.tool ?? null,
      result: entry.toolResult,
    }));

  const summary = {
    timestamp: new Date().toISOString(),
    sessionId,
    prompt,
    tool: tools,
    actionTook,
    error: errors.length ? errors : null,
    toolResult,
  };

  if (prompt) {
    appendJsonl(aggregateFile, summary);
  }

  cleanSession(sessionDir);
}
