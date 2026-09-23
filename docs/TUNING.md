# Horde tuning (PRO-28)

The horde director paces a street with a tension meter from 0 to 100. Gunfire and explosions add 12, a suppressed shot 4 and a sprint step 1. The meter drains 1.2 a second, or 4 a second in Relax. It reads as Calm up to 15, Relax to 40, BuildUp to 75 and Peak above that.

Each spawn tick (the district's interval times the difficulty's interval scale) asks the spawner for the state's batch. Spawns happen outside the camera frustum, at least 14 m from the leader and never in the sanctuary. A loud shot also brings 2 reinforcements, at most once every 20 s.

## Target

On **Survivor** a street should give the leader about **one kill every 10 seconds**, which is 30 bodies over five minutes. The scripted five-minute run (`PressureClock.Run`: an opening tension of 20, an 8 s base interval, tier 2, firefights at 25 s and 170 s, daytime) spawns:

| Difficulty | Bodies in 5 min | Per kill |
|---|---|---|
| Scavenger | 16 | about 19 s |
| Survivor | 31 | about 10 s |
| Nightmare | 52 | about 6 s |

`TensionLogTests.ASurvivorStreetFeedsAboutOneKillEveryTenSeconds` holds Survivor between 27 and 33 and Nightmare above 1.5 times Survivor. A night street adds the runner pack (4 more).

`TensionCurveTests` (PlayMode) plays the same five minutes in the arena and writes `Artifacts/Perf/tension-curve.csv`: seconds, tension, state and zombies alive every 5 s. Plot it to check the meter climbs at each firefight and drains in between.

## Difficulty rows

Each level is an asset in `Assets/Data/Difficulty`, loaded through `Resources/DifficultyBook.asset`. **Tools > Outpost Zero > Sync Difficulty Book** writes missing levels from the built-in rows, and `DifficultyBookTests` holds the committed assets equal to them.

| Field | Scavenger | Survivor | Nightmare | What it does |
|---|---|---|---|---|
| Tension per tier | 2 | 4 | 8 | Opening tension added per threat tier above 1 |
| Tension per day | 0 | 0 | 0.5 | Opening tension added per day survived |
| Interval scale | 1.15 | 1 | 0.75 | Multiplies the district's spawn interval |
| Extra kills | 0 | tier − 1 | tier + 1 | Added to the street's kill quota |
| Runner / brute tier | never | 2 / 3 | 2 / 3 | Tier from which the opening crowd leans to runners, then brutes |
| BuildUp / Peak / Relax batch | 1 / 3 / 1 | 2 / 4 / 1 | 3 / 6 / 1 | Bodies per spawn tick in each state |
| Alive scale | 0.75 | 1 | 1 | Share of the quality tier's crowd cap |
| Walker / runner / brute weight | 50 / 35 / 15 | 34 / 33 / 33 | 25 / 40 / 35 | Spawn mix |
| Cooldown scale | 1.25 | 1 | 0.8 | Runner lunge and brute charge cooldown (4 s base) |
| Loot scale | 1.25 | 1 | 0.75 | Every container's rolled counts |

The Low quality tier caps the crowd at 16 whatever the difficulty.

## Timed beats

- 45 s: a mini-horde of 8 walkers from one alley.
- 90 s on, at night (20:00 to 05:00), from tier 2: a pack of 4 runners, once per street.
- 5 minutes, from tier 3: a brute ambush.
