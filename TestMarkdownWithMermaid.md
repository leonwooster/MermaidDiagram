# System Architecture Documentation

This document demonstrates **Markdown with embedded Mermaid diagrams** - a powerful combination for technical documentation.

## Overview

Our system follows a microservices architecture with clear separation of concerns.

## System Components

```mermaid
graph TB
    Client[Client Application]
    API[API Gateway]
    Auth[Authentication Service]
    User[User Service]
    Order[Order Service]
    DB[(Database)]
    
    Client --> API
    API --> Auth
    API --> User
    API --> Order
    User --> DB
    Order --> DB
    Auth --> DB
```

The diagram above shows the high-level architecture of our system.

## User Authentication Flow

Here's how users authenticate in our system:

```mermaid
sequenceDiagram
    participant User
    participant Client
    participant API
    participant Auth
    participant DB
    
    User->>Client: Enter credentials
    Client->>API: POST /login
    API->>Auth: Validate credentials
    Auth->>DB: Query user
    DB-->>Auth: User data
    Auth-->>API: JWT Token
    API-->>Client: Token + User info
    Client-->>User: Login successful
```

## Data Model

Our core entities are represented in this class diagram:

```mermaid
classDiagram
    class User {
        +String id
        +String email
        +String name
        +DateTime createdAt
        +login()
        +logout()
    }
    
    class Order {
        +String id
        +String userId
        +Decimal total
        +OrderStatus status
        +DateTime createdAt
        +addItem()
        +checkout()
    }
    
    class OrderItem {
        +String id
        +String orderId
        +String productId
        +int quantity
        +Decimal price
    }
    
    User "1" --> "*" Order
    Order "1" --> "*" OrderItem
```

## Deployment States

```mermaid
stateDiagram-v2
    [*] --> Development
    Development --> Testing
    Testing --> Staging
    Staging --> Production
    
    Testing --> Development: Failed Tests
    Staging --> Testing: Issues Found
    Production --> [*]
```

## Key Features

- **Hybrid Rendering**: Seamlessly combines Markdown text with Mermaid diagrams
- **Multiple Diagram Types**: Supports flowcharts, sequence diagrams, class diagrams, and more
- **Live Preview**: Real-time rendering as you type
- **Export Options**: Save as PNG, SVG, or PDF

## Conclusion

This document demonstrates that Markdown and Mermaid diagrams work together perfectly, enabling rich technical documentation with visual elements.
