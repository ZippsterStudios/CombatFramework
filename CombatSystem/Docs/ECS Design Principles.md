# ECS Design Principles

Pure policies, write-only drivers, deterministic math via Unity.Mathematics.
Systems implement ISystem and update in SimulationSystemGroup.
No managed allocations inside hot loops; use FixedStringXXBytes for identifiers.
