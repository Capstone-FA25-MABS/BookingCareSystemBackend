%% Mermaid system architecture for microservices project
%% Paste this into a Mermaid live editor (or supported markdown) to render

flowchart LR
  %% Actors
  User["User / Browser"]

  subgraph CDN & Frontend
    direction TB
    CF["AWS CloudFront (CDN)"]
    S3["AWS S3 (Static files: React + TS)"]
    ReactApp["React + TypeScript (static)"]
  end

  %% Networking / API
  subgraph Edge
    direction TB
    Ingress["Ingress Controller / API Gateway"]
    TLS["TLS / WAF"]
  end

  %% Kubernetes cluster with microservices
  subgraph Kubernetes_Cluster[Kubernetes Cluster (EKS / self-hosted)]
    direction TB
    K8sIngress["K8s Ingress Controller"]

    subgraph Services[Microservices (ASP.NET Core)]
      direction LR
      SvcA["Service A\n(ASP.NET Core)"]
      SvcB["Service B\n(ASP.NET Core)"]
      SvcC["Service C\n(ASP.NET Core)"]
    end

    %% Databases
    subgraph Databases[SQL Server Databases]
      direction TB
      DBA["SQL Server - DB_A (Service A)"]
      DBB["SQL Server - DB_B (Service B)"]
      DBC["SQL Server - DB_C (Service C)"]
    end

    %% Shared infra inside cluster
    Redis["Redis Cluster (caching)"]
    Prom["Prometheus (metrics)"]
    Jaeger["Jaeger (tracing)"]
    Fluent["Fluentd / Filebeat -> ELK (centralized logs)"]
  end

  %% CI/CD & Registry
  subgraph CI_CD[CI / CD]
    direction TB
    GHActions["GitHub Actions (CI/CD)"]
    Registry["Container Registry (ECR / ACR / DockerHub)"]
  end

  %% Observability UI
  subgraph Observability
    direction TB
    Grafana["Grafana (dashboards)"]
    Kibana["Kibana (logs)"]
  end

  %% Connections: User to Frontend
  User --> CF
  CF --> S3
  CF --> ReactApp
  ReactApp -->|HTTPS / REST/gRPC| CF

  %% Frontend to backend
  ReactApp -->|HTTPS| Ingress
  CF -->|Edge caching / invalidation| Ingress
  Ingress --> K8sIngress
  K8sIngress --> Services

  %% Service interactions
  Services -->|reads/writes| DBA
  Services -->|reads/writes| DBB
  Services -->|reads/writes| DBC

  Services -->|cache get/set| Redis
  Services -->|emit metrics| Prom
  Services -->|send traces| Jaeger
  Services -->|send logs| Fluent

  %% Prometheus + Grafana
  Prom --> Grafana
  Fluent --> Kibana
  Jaeger --> Grafana

  %% CI/CD flow
  GHActions -->|build & test| Registry
  Registry -->|image pull| Kubernetes_Cluster
  GHActions -->|deploy (kubectl / helm)| Kubernetes_Cluster

  %% Extra infra
  CF ---|origin| S3
  TLS ---|protect| Ingress

  %% Notes and grouping
  classDef infra fill:#f3f4f6,stroke:#aaa,stroke-width:1px
  class Kubernetes_Cluster,CDN & Frontend,CI_CD,Observability infra

  %% Legend (optional)
  subgraph Legend
    direction LR
    L1["-> : request/flow"]
    L2["-- : association/origin"]
  end

  style User fill:#fff,stroke:#333
  style CF fill:#eef2ff,stroke:#3b82f6
  style S3 fill:#fff,stroke:#8b5cf6
  style ReactApp fill:#f0fff4,stroke:#10b981
  style Ingress fill:#fff7ed,stroke:#f97316
  style K8sIngress fill:#ecfeff,stroke:#06b6d4
  style Services fill:#fff,stroke:#000
  style Redis fill:#fff7f0,stroke:#fb923c
  style Prom fill:#f8fafc,stroke:#0ea5a4
  style Jaeger fill:#fff1f2,stroke:#fb7185
  style GHActions fill:#eef2ff,stroke:#6366f1
  style Registry fill:#fff,stroke:#374151
  style Grafana fill:#fff,stroke:#f97316
  style Kibana fill:#fff,stroke:#f59e0b

  %% End
