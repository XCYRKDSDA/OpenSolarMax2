# OpenSolarMax2

This project aims to rebuild the games SolarMax2 and SolarMax3, which have been discontinued for maintenance.

## Background

The original SolarMax2 game was developed by Nico Tuason and launched on June 7, 2014. Unfortunately, it is no longer compatible with newer versions of Android. Its sequel, SolarMax3, developed by Xiaomi, was an online game that required a server connection to play, but the servers have long been discontinued.

Despite this, there remains an active community of players in China. This community has reverse-engineered and modified SolarMax3, enabling local PvE gameplay without the need for a server connection. Additionally, they have found ways to create custom levels, allowing the game’s content to continue evolving.

Given the aforementioned background, there are two main reasons for creating this project. One reason is for my personal programming practice. The other is my desire to deeply expand the existing game mechanics. Currently, the community-modified version of the game still relies on existing entities, logic, and AI. In the future, I hope to fully customize and develop new entities, logic, and AI.

The number "2" in the repository's name does not signify a remake of the Solarmax**2**. Instead, it indicates that this is my second reconstruction attempt. The initial version has became difficult to maintain due to my lack of experience and inadequate technical skills.

## Roadmap

The project is modularly designed: a launcher loads different modules to introduce different mechanics. Currently, I divide the mechanics in the SolarMax game into three parts: core mechanics and feature mechanics for SolarMax2 and SolarMax3.

### Launcher

- [*] Mod Loading
- [*] Level packages and level loading
- [ ] Mod/Level package managed by git
- [ ] Level background
- [ ] UI for packages management
- [ ] UI for level selection

### Core Mechanics

- [*] Unit production
- [*] Unit combat
- [*] Unit Shipping
- [*] Unit Revolution
- [*] Planet colonization
- [*] Planet revolution
- [ ] Barrier
- [ ] Basic AI
- [*] Visualization of unit combat status
- [*] Visualization of planet colonization
- [*] Input system for shipping units
- [ ] Sound system and effects

### SolarMax2 Features

- [ ] Portal
- [ ] Turret
- [ ] Fortress

### SolarMax3 Features

- [ ] Blackhole
- [ ] Pulser
- [ ] Transformer
- [ ] Capture Tower
- [ ] Replicator

### Game Assets

The art assets currently used in the project are obtained by unpacking existing games' packages. The plan is to replace them with my own made or open source assets in the future.

## Build and run

> This project is currently in the early stages of development, and many features are very unstable. There is no guarantee that it will always compile successfully and run correctly.

Regardless of which IDE is used, both "OpenSolarMax.Launcher" and "OpenSolarMax.Mods.Core" must be compiled separately. This is because the former does not explicitly depend on the latter. Instead, the launcher loads mods by loading the assembly directly at runtime. Once compiled, you can run the launcher project to start the game.
