# Combat System Architecture Overview

This folder contains integration glue for the modular ECS subsystems. Each feature area (e.g., Buffs, DOT, HOT) owns its Requests → Policies → Drivers → Features → Runtime → Telemetry pipeline and registers via SubsystemManifestRegistry.

