# Performance Optimizations

HermesProxy has been extensively optimized to minimize latency and memory allocations in packet handling hot paths.

## Span-Based Packet I/O (Zero-Allocation)

The packet serialization system uses `Span<T>` and `ref struct` types for zero-allocation packet writing and reading:

**SpanPacketWriter vs ByteBuffer (Write Operations)**

| Operation    | ByteBuffer | SpanWriter | Speedup | Memory      |
|--------------|------------|------------|---------|-------------|
| WriteInt64   | 93.37 ns   | 0.29 ns    | ~317x   | 80B → 0B    |
| WriteVector3 | 102.99 ns  | 0.68 ns    | ~151x   | 88B → 0B    |
| WriteMixed   | 109.30 ns  | 1.29 ns    | ~85x    | 96B → 0B    |

**SpanPacketReader vs ByteBuffer (Read Operations)**

| Operation    | ByteBuffer | SpanReader | Speedup  | Memory      |
|--------------|------------|------------|----------|-------------|
| ReadInt64    | 157.98 ns  | 0.08 ns    | ~1948x   | 48B → 0B    |
| ReadVector3  | 178.31 ns  | 0.75 ns    | ~238x    | 48B → 0B    |
| ReadCString  | 294.61 ns  | 23.51 ns   | ~12.5x   | 104B → 56B  |

## ByteBuffer Optimizations

The core `ByteBuffer` class has been refactored for improved performance:
- ArrayPool-based buffer management reduces GC pressure
- Direct `BinaryPrimitives` usage eliminates BinaryReader/BinaryWriter overhead
- `MemoryStream.ToArray()` optimization for `GetData()`:

| Buffer Size | Original     | Optimized   | Speedup |
|-------------|--------------|-------------|---------|
| Small       | 46.87 ns     | 10.31 ns    | ~4.5x   |
| Medium      | 649.49 ns    | 70.88 ns    | ~9.2x   |
| Large       | 36,383.19 ns | 4,234.92 ns | ~8.6x   |

## Additional Optimizations

- **Enum Conversions**: Cached name-based mappings replace `Enum.Parse(typeof(T), x.ToString())` pattern (8-25x speedup, 95% memory reduction)
- **Opcode Lookups**: `FrozenDictionary` for O(1) opcode resolution
- **WowGuid**: Refactored to value-type record structs eliminating heap allocations
- **NetworkThread**: O(1) socket removal with `ConcurrentQueue`
- **BnetTcpSession**: Zero-allocation buffer management with `Span<T>`
- **Movement Handlers**: Fixed monster/pet movement zig-zag at tile boundaries
