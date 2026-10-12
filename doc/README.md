# Cestyll.Ecs docs

The examples use `Registry`, a project-wide alias for `Registry<Entity32>`. See [Registry](registry.md#registry-alias).

## Core

- [Registry](registry.md): creating and destroying entities, components, context, bulk calls, `Entity32` and `Entity64`
- [Entity handles](entity-handles.md): the `EntityHandle` that `Create()` returns, for chaining calls on one entity
- [Storage](storage.md): pools, named storage and sorting
- [Tags](tags.md): data-less flags stored as one bit per entity

## Iteration

- [Views](views.md): walking entities by component, with filters, pinning and joins
- [Groups](groups.md): owning, nested and non-owning groups
- [SIMD](simd.md): walking pools and groups as `Vector256` lanes

## Links between entities

- [Relations](relations.md): exclusive relations and many-to-many link sets
- [Attachments](attachments.md): keyed sub-items per entity
- [Inboxes](inboxes.md): messages posted to an entity, read back in posting order

## Reacting to change

- [Observers and signals](observers.md): change signals on every store, and observers that collect changed entities
- [Requirements](requirements.md): rules that add a component to entities that gain another
- [Events](events.md): a typed dispatcher with immediate and queued delivery
- [Command buffers](commands.md): recording changes and applying them later, `Defer()` and `AutoDefer`

## Persistence

- [Snapshots](snapshots.md): saving and loading a registry through an archive you write
- [JSON snapshots](snapshots-json.md): a snapshot archive over `System.Text.Json`
