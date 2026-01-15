# Test Document with Mermaid Diagram

This is a test document to verify that Mermaid diagrams render properly in Word export.

## Sample Flowchart

```mermaid
graph TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
```

## Another Diagram

```mermaid
sequenceDiagram
    participant Alice
    participant Bob
    Alice->>Bob: Hello Bob, how are you?
    Bob-->>Alice: I am good, thanks!
```

This should render both diagrams properly in the exported Word document.