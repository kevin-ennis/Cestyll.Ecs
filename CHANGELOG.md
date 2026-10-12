# Changelog

All notable changes to this project are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

## [0.7.0] - Unreleased

First public release.

### Added

- Registry over `Entity32` (20-bit index, 12-bit version) or `Entity64`, with handles, bulk create and destroy, and custom entity layouts
- Sparse-set storage: one packed pool per component type, plus named pools
- Views of 1 to 16 components with With/Without filters, tag filters, pinning, joins, `Each` and `foreach`
- Groups: owning, nested and non-owning, with sorting
- Tags: one bit per entity, never a pool
- Relations (`Relate`/`Unrelate`, one target per source) and link sets (many targets, data on each link), with reverse lookup and target-destroyed policies
- Attachments: keyed sub-items per entity
- Inboxes: messages posted to an entity, read back in posting order
- Observers, signals and requirements
- Command buffers, defer scopes and `AutoDefer` walks
- Snapshots through user-written archives, including continuous loading
- Event dispatcher with immediate and queued delivery
- SIMD walks over pools and owning groups as `Vector256` lanes
- Safe managed code only: no `unsafe`, no reflection, trimming and NativeAOT compatible

[0.7.0]: https://github.com/kevin-ennis/Cestyll.Ecs/releases/tag/v0.7.0
