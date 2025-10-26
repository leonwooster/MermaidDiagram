# Epic: AI Agent-Assisted Mermaid Diagram Generation

## Summary
Introduce an AI-driven experience that translates user prompts into fully formed Mermaid diagrams. The system should automatically determine the most suitable diagram type (e.g., flowchart, sequence, network) based on the prompt's intent, and support pluggable LLM backends including both managed cloud providers and a locally hosted Ollama instance. Generated diagrams can be directly imported into the Visual Diagram Builder for further visual manipulation through drag-and-drop rearrangement, styling, and refinement.

## Problem / Opportunity Statement
* Crafting Mermaid diagrams remains manual and error-prone, particularly for complex infrastructure or architecture content.
* Users need a faster way to go from conceptual description to diagram without mastering Mermaid syntax nuances.
* Providing both cloud and local AI options unlocks flexibility for security-conscious environments.

## Goals
* Deliver a natural-language prompt workflow that selects the best-fitting Mermaid diagram type automatically.
* Allow administrators to configure the AI backend, choosing between external LLM APIs and local Ollama deployments.
* Provide preview, iteration, and validation capabilities before injecting generated diagrams into the editor.
* Enable seamless transition from AI-generated Mermaid code to interactive visual canvas for refinement.
* Leverage existing Visual Diagram Builder infrastructure for canvas manipulation and code synchronization.

## Non-Goals
* Building bespoke LLM models; focus on orchestration and UX around existing providers.
* Implementing code generation or non-diagram artifacts.
* Offline inference without a local Ollama host.

## User Journey
1. User opens the "AI Diagram Assistant" panel from the toolbar or Templates menu.
2. User selects a prompt preset (e.g., AWS topology, microservices) or enters a free-form request.
3. System routes the prompt to the configured LLM backend (cloud or Ollama) along with domain metadata.
4. AI agent interprets the prompt, determines the optimal Mermaid diagram type, and returns syntax plus rationale.
5. User reviews rendered preview, inspects generated code, and optionally refines the prompt.
6. Once satisfied, user can either:
   * Insert the diagram directly into the main editor as code, or
   * Import the diagram into the Visual Diagram Builder canvas for visual manipulation
7. In the Visual Diagram Builder, user can rearrange elements via drag-and-drop, adjust styling, and refine connections.
8. Changes in the visual canvas automatically update the Mermaid code representation.

## High-Level Requirements
* UI enhancements for prompt input, backend selection, and preset management.
* Backend orchestration layer that abstracts LLM providers with support for:
  * Azure OpenAI / OpenAI API or similar managed services.
  * Local Ollama instance for on-prem/offline inference.
* Diagram type classifier leveraging LLM reasoning plus heuristics.
* Mermaid syntax validation pipeline with clear error messaging.
* Iteration history with ability to revert or compare previous generations.
* Integration with Visual Diagram Builder for importing generated code into visual canvas.
* Bidirectional synchronization between AI-generated code and visual canvas manipulations.
* Telemetry capturing prompt usage, backend selection, and success/error rates.

## Acceptance Criteria
* Prompt-to-diagram flow completes within three user actions (prompt, submit, insert) for happy path.
* System identifies correct diagram type for at least 80% of curated test prompts.
* Switching between cloud and Ollama backends is available via settings without app restart.
* Generated Mermaid is syntactically valid or emits actionable corrections.
* Undo/redo integration functions after diagram insertion.

## Dependencies & Integrations
* Existing rendering services in `MermaidDiagramApp/Services/Rendering/` for preview and validation.
* Configuration management for storing API keys or Ollama endpoints securely.
* Network access policies for calling external LLMs (when enabled).
* Feature flagging infrastructure to control rollout.
* Visual Diagram Builder infrastructure in `MermaidDiagramApp/Views/` and `MermaidDiagramApp/ViewModels/` for canvas integration.
* Existing code generation and parsing mechanisms in `DiagramCanvasViewModel.cs`.

## Milestones
* __Discovery__: Evaluate diagram classification strategies, shortlist LLM providers, prototype Ollama integration.
* __MVP__: Implement prompt UI, single backend support, basic classification, preview & insert flow.
* __Beta__: Add backend switcher, prompt presets, telemetry dashboards, iteration history.
* __GA__: Harden validation, add cost controls, finalize governance/security reviews.

## Risks & Mitigations
* __LLM hallucination__: Incorporate post-generation validation and user-visible reasoning snippets.
* __Backend latency__: Cache frequent prompts, allow async generation with notifications.
* __Security concerns__: Provide on-device Ollama option, encrypt credentials, allow tenant-level policy enforcement.
* __Diagram mismatch__: Maintain curated test suite and allow manual override of diagram type before insertion.

## Open Questions
* Do we require per-tenant throttling or quota management for LLM usage?
* How should we expose configuration for custom prompt templates to power users?
* Will we need a fallback experience if both cloud and local backends are unavailable?

## Success Metrics
* 70% of generated diagrams inserted without manual syntax edits.
* <=5% of sessions encounter backend errors (per backend type).
* Feature NPS  +30 among beta participants.
* Increase in diagram creation velocity (prompt-to-canvas) by 50% compared to manual baseline.

---

## Floating AI Prompt Overlay (UX Requirements)

### Summary
Provide a movable floating AI prompt panel that remains visible above editor/preview content, with predictable drag behavior. Allow users to drag the panel within the app window boundaries; when dragging beyond the app window (e.g., toward another monitor), offer a pop-out to a separate window so it can be placed on a second screen.

### Requirements
* Topmost overlay within the app content area so it is not obscured by the preview or editor controls.
* Minimum width sufficient to display all header controls and action buttons without clipping.
* Dragging is clamped within the main window bounds; the panel cannot be lost by dragging outside the visible area.
* When the user drags significantly beyond the window edge, offer a “pop out” behavior to open the floating prompt in a separate window that the user can move to another screen.
* Provide an explicit “Pop out” button to detach on demand.

### Acceptance Criteria
* The floating panel stays above the editor and preview content at all times.
* The floating panel shows all controls without truncation at default scale/DPI.
* Drag gestures within the app keep the panel inside the content area and do not allow it to disappear off-screen.
* Dragging far beyond the edge (or pressing the Pop out button) opens the AI prompt in a separate window. Closing the window restores the docked floating panel.
* Multi-monitor placement is supported via the pop-out window.
