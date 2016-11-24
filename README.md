# InfiniMap

InfiniMap is a map library capable of expressing sparsely stored chunk based maps. 

## Using Infinimap

Examples live in InfiniMap.Text but the basic gist of the library is:

```csharp
// Create a new 3D Map (there is also Map2D)
var map = new Map3D<float>();

// Put something at (0,0,0)
map[0, 0, 0] = 1.0f;        // Chunk: (0,0,0)

// Put something at (16,16,16)
map[16, 16, 16] = 2.0f;     // Chunk: (1,1,1)

// Put something at (32,32,32)
map[32, 32, 32] = 4.0f;     // Chunk: (2,2,2)
```

Maps can store anything as they are a generic container, and are expandable above three dimensions if required, although storage per chunk increases as a result.

### Saving/Loading data

When infinimap generates a new chunk, it invokes a user registerable callback provided via `RegisterWriter(...)` which provides the chunk itself as a sequence and the chunk coordinates, there is also a `RegisterReader(...)` which provides chunk coordinates and expects a sequence in return.

### Entity Storage

For managing entities; non-tile data; inside a chunk, the methods `PutEntity(...)`, `MoveEntity(...)`, `GetEntitiesInChunk(...)`, `GetEntitiesAt(...)` and `RemoveEntity(...)` are provided for managing entities at a chunk level, the only requirement for an entity to be managed with this system is that it implement `IEntityLocationData` for coordinate tracking.

### coordinate Systems

There are three coordinate systems in use, Chunk, Item, and World.

#### Chunk-Space
A coordinate of a chunk among other chunks, the center of the world is chunk (0,0,0) the chunk sitting on top of that to it would be (0,0,1)

#### World-Space
A coordinate of an item among other items, the center of world is (0,0,0) and an item directly ontop of it would be (0,0,1). An item 63 s away on the Y plane would be (0,63,1)

#### Item-Space
A coordinate of an item inside a block, translated from d-space. The item at (worldspace) (0,0,1) exists in the chunk space of (0,0,0) and the block space of (0,0,1). An item at (63,0,0) in the world exists in chunkspace at (3,0,0) and itemspace of (15,0,0)