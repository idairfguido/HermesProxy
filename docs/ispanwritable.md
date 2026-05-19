# ISpanWritable Implementation Status

This document tracks the progress of implementing `ISpanWritable` interface for server packets to enable zero-allocation span-based packet writing.

## Current Progress

| Metric | Count |
|--------|-------|
| **Packets WITH ISpanWritable** | 272 |
| **Packets WITHOUT ISpanWritable** | 49 |
| **Total ServerPackets** | 321 |
| **Coverage** | 84.7% |

## Converted Packets (272)

<details>
<summary>Click to expand full list</summary>

### AuthenticationPackets.cs (3)
- `Pong`
- `WaitQueueFinish`
- `ResumeComms`

### AuctionPackets.cs (5)
- `AuctionHelloResponse`
- `AuctionClosedNotification` *(uses AuctionPacketHelpers for ItemInstance)*
- `AuctionOwnerBidNotification` *(uses AuctionPacketHelpers for ItemInstance)*
- `AuctionWonNotification` *(uses AuctionPacketHelpers for ItemInstance)*
- `AuctionOutbidNotification` *(uses AuctionPacketHelpers for ItemInstance)*

### BattleGroundPackets.cs (5)
- `BattlegroundInit`
- `BattlegroundPlayerLeftOrJoined`
- `AreaSpiritHealerTime`
- `PvPCredit`
- `PlayerSkinned`

### CharacterPackets.cs (13)
- `CreateChar`
- `DeleteChar`
- `LoginVerifyWorld`
- `CharacterLoginFailed`
- `LogoutResponse`
- `LogoutComplete`
- `LogoutCancelAck`
- `LogXPGain`
- `PlayedTime`
- `InspectHonorStatsResultClassic`
- `InspectHonorStatsResultTBC`
- `GenerateRandomCharacterNameResult` *(bounded string - player name)*
- `CharacterRenameResult` *(bounded string - player name)*

### ChatPackets.cs (2)
- `STextEmote`
- `ChatPlayerNotfound` *(bounded string - player name)*

### ClientConfigPackets.cs (1)
- `ClientCacheVersion`

### CombatPackets.cs (6)
- `AttackSwingError`
- `SAttackStart`
- `SAttackStop`
- `CancelCombat`
- `AIReaction`
- `PartyKillLog`

### DuelPackets.cs (7)
- `CanDuelResult`
- `DuelRequested`
- `DuelCountdown`
- `DuelComplete`
- `DuelInBounds`
- `DuelOutOfBounds`
- `DuelWinner` *(bounded string - 2 player names)*

### GameObjectPackets.cs (5)
- `GameObjectDespawn`
- `GameObjectResetState`
- `GameObjectCustomAnim`
- `FishNotHooked`
- `FishEscaped`

### GroupPackets.cs (12)
- `GroupUninvite`
- `ReadyCheckStarted`
- `ReadyCheckResponse`
- `ReadyCheckCompleted`
- `SendRaidTargetUpdateSingle`
- `SendRaidTargetUpdateAll` *(capped 8 raid markers)*
- `SummonRequest`
- `MinimapPing`
- `RandomRoll`
- `GroupDecline` *(bounded string - player name)*
- `GroupNewLeader` *(bounded string - player name)*
- `PartyCommandResult` *(bounded string - player name)*

### GuildPackets.cs (16)
- `GuildCommandResult` *(bounded string - guild/player name)*
- `GuildSendRankChange`
- `GuildEventDisbanded`
- `GuildEventRanksUpdated`
- `GuildEventTabAdded`
- `GuildEventBankMoneyChanged`
- `GuildEventTabTextChanged`
- `PlayerTabardVendorActivate`
- `PlayerSaveGuildEmblem`
- `GuildBankRemainingWithdrawMoney`
- `GuildEventPlayerJoined` *(bounded string - player name)*
- `GuildEventPlayerLeft` *(bounded string - player names)*
- `GuildEventNewLeader` *(bounded string - 2 player names)*
- `GuildEventPresenceChange` *(bounded string - player name)*
- `GuildInviteDeclined` *(bounded string - player name)*

### InstancePackets.cs (8)
- `UpdateInstanceOwnership`
- `UpdateLastInstance`
- `InstanceReset`
- `InstanceResetFailed`
- `ResetFailedNotify`
- `InstanceSaveCreated`
- `RaidGroupOnly`
- `RaidInstanceMessage`

### ItemPackets.cs (13)
- `SetProficiency`
- `BuySucceeded`
- `BuyFailed`
- `SellResponse`
- `ReadItemResultFailed`
- `ReadItemResultOK`
- `SocketGemsSuccess`
- `DurabilityDamageDeath`
- `ItemCooldown`
- `ItemEnchantTimeUpdate`
- `EnchantmentLog`
- `ItemPushResult` *(uses ItemPacketHelpers for ItemInstance)*
- `InventoryChangeFailure` *(conditional fields based on BagResult)*

### LootPackets.cs (8)
- `LootReleaseResponse`
- `LootMoneyNotify`
- `CoinRemoved`
- `LootRemoved`
- `LootRollsComplete`
- `MasterLootCandidateList` *(capped 40 raid members)*
- `LootList` *(optional fields)*
- `LootResponse` *(capped 16 items, 4 currencies)*

### MailPackets.cs (2)
- `NotifyReceivedMail`
- `MailCommandResult`

### MiscPackets.cs (22)
- `BindPointUpdate`
- `PlayerBound`
- `ServerTimeOffset`
- `TutorialFlags`
- `CorpseReclaimDelay`
- `TimeSyncRequest`
- `WeatherPkt`
- `StartLightningStorm`
- `LoginSetTimeSpeed`
- `AreaTriggerMessage`
- `DungeonDifficultySet`
- `InitialSetup`
- `CorpseLocation`
- `DeathReleaseLoc`
- `StandStateUpdate`
- `ExplorationExperience`
- `PlayMusic`
- `PlaySound` *(version-specific)*
- `PlayObjectSound` *(version-specific)*
- `TriggerCinematic`
- `StartMirrorTimer`
- `PauseMirrorTimer`
- `StopMirrorTimer`
- `ConquestFormulaConstants`
- `SeasonInfo`
- `InvalidatePlayer`
- `ZoneUnderAttack`

### MovementPackets.cs (17)
- `MonsterMove` *(capped 64 waypoints, real usage: 1-2 points)*
- `MoveUpdate`
- `MoveTeleport` *(optional Vehicle + TransportGUID)*
- `TransferPending` *(optional Ship + TransferSpellID)*
- `TransferAborted`
- `NewWorld`
- `MoveSplineSetSpeed`
- `MoveSetSpeed`
- `MoveUpdateSpeed`
- `MoveSplineSetFlag`
- `MoveSetFlag`
- `MoveSetCollisionHeight`
- `MoveKnockBack`
- `MoveUpdateKnockBack`
- `SuspendToken`
- `ResumeToken`
- `ControlUpdate`

### NPCPackets.cs (6)
- `GossipComplete` *(version-specific)*
- `BinderConfirm`
- `ShowBank`
- `TrainerBuyFailed`
- `RespecWipeConfirm`
- `SpiritHealerConfirm`

### PetitionPackets.cs (3)
- `PetitionSignResults`
- `TurnInPetitionResult`
- `PetitionRenameGuildResponse` *(bounded string - guild name)*

### PetPackets.cs (3)
- `PetClearSpells`
- `PetActionSound`
- `PetStableResult`

### QueryPackets.cs (2)
- `QueryTimeResponse`
- `QueryPetNameResponse` *(bounded string - pet name + declined names)*

### QuestPackets.cs (5)
- `QuestGiverQuestFailed`
- `QuestUpdateStatus`
- `QuestUpdateAddCredit`
- `QuestUpdateAddCreditSimple`
- `QuestPushResult`

### ReputationPackets.cs (1)
- `SetFactionVisible`

### SessionPackets.cs (1)
- `ConnectionStatus`

### SpellPackets.cs (28)
- `CancelAutoRepeat`
- `SpellPrepare`
- `CastFailed`
- `PetCastFailed`
- `SpellFailure`
- `SpellFailedOther`
- `CooldownEvent`
- `ClearCooldown`
- `CooldownCheat`
- `SpellDelayed`
- `SpellChannelStart` *(optional InterruptImmunities + HealPrediction)*
- `SpellChannelUpdate`
- `SpellInstakillLog`
- `PlaySpellVisualKit`
- `TotemCreated`
- `ResurrectRequest` *(bounded string - player name)*
- `LearnedSpells` *(capped 8 spells)*
- `UnlearnedSpells` *(capped 8 spells)*
- `SendUnlearnSpells` *(capped 8 spells)*
- `SpellCooldownPkt` *(capped 64 cooldowns)*
- `SendSpellHistory` *(capped 64 entries)*
- `SendSpellCharges` *(capped 16 entries)*
- `SpellEnergizeLog` *(optional SpellCastLogData, capped 10 power entries)*
- `SpellDamageShield` *(optional SpellCastLogData, capped 10 power entries)*
- `EnvironmentalDamageLog` *(optional SpellCastLogData, capped 10 power entries)*
- `SpellHealLog` *(optional SpellCastLogData + ContentTuning, high-frequency)*
- `SpellNonMeleeDamageLog` *(optional SpellCastLogData + ContentTuning, high-frequency)*
- `SetSpellModifier` *(capped 8 modifiers × 8 data entries, high-frequency)*

### ArenaPackets.cs (2)
- `ArenaTeamCommandResult` *(bounded string - team + player names)*
- `ArenaTeamInvite` *(bounded string - team + player names)*

### TaxiPackets.cs (3)
- `TaxiNodeStatusPkt`
- `NewTaxiPath`
- `ActivateTaxiReplyPkt`

### UpdatePackets.cs (1)
- `PowerUpdate` *(capped 16 power types)*

### WorldStatePackets.cs (1)
- `UpdateWorldState`

</details>

## Unconverted Packets (49)

These packets cannot implement `ISpanWritable` due to dynamic lists or complex data structures that cannot determine `MaxSize` at compile time.

<details>
<summary>Dynamic Lists / Complex Data</summary>

| Packet | File | Blocking Field(s) |
|--------|------|-------------------|
| `AuctionListMyItemsResult` | AuctionPackets.cs | `List<AuctionItem>` |
| `AuctionListItemsResult` | AuctionPackets.cs | `List<AuctionItem>` |
| `PVPMatchStatisticsMessage` | BattleGroundPackets.cs | `List<PVPMatchPlayerStatistics>` |
| `EnumCharactersResult` | CharacterPackets.cs | `List<CharacterListEntry> Characters` |
| `GetAccountCharacterListResult` | CharacterPackets.cs | `List<AccountCharacterListEntry>` |
| `InspectResult` | CharacterPackets.cs | Multiple lists (talents, glyphs, items) |
| `AttackerStateUpdate` | CombatPackets.cs | Complex damage info with lists |
| `PartyUpdate` | GroupPackets.cs | `List<PartyPlayerInfo> PlayerList` |
| `PartyMemberPartialState` | GroupPackets.cs | Complex nested state |
| `PartyMemberFullState` | GroupPackets.cs | Complex nested state |
| `QueryGuildInfoResponse` | GuildPackets.cs | Multiple rank names |
| `GuildRoster` | GuildPackets.cs | `List<GuildRosterMemberData>` |
| `GuildRanks` | GuildPackets.cs | `List<GuildRankData>` |
| `GuildBankQueryResults` | GuildPackets.cs | `List<GuildBankItemInfo>` |
| `GuildBankLogQueryResults` | GuildPackets.cs | `List<GuildBankLogEntry>` |
| `DBReply` | HotfixPackets.cs | Dynamic data blob |
| `AvailableHotfixes` | HotfixPackets.cs | `List<HotfixRecord>` |
| `HotfixConnect` | HotfixPackets.cs | `List<HotfixRecord>` |
| `HotFixMessage` | HotfixPackets.cs | `List<HotfixData>` |
| `MailListResult` | MailPackets.cs | `List<MailListEntry>` |
| `GossipMessagePkt` | NPCPackets.cs | `List<GossipOption>`, `List<GossipQuest>` |
| `VendorInventory` | NPCPackets.cs | `List<VendorItem>` |
| `PetSpells` | PetPackets.cs | `List<uint> ActionButtons`, `List<PetSpellCooldown>` |
| `QueryQuestInfoResponse` | QueryPackets.cs | Complex quest data with lists |
| `QueryCreatureResponse` | QueryPackets.cs | Variable-length strings |
| `QueryGameObjectResponse` | QueryPackets.cs | Variable-length strings |
| `QueryPageTextResponse` | QueryPackets.cs | Variable-length text |
| `WhoResponsePkt` | QueryPackets.cs | `List<WhoEntry>` |
| `QuestGiverQuestDetails` | QuestPackets.cs | Multiple lists (rewards, objectives) |
| `QuestGiverQuestListMessage` | QuestPackets.cs | `List<GossipQuest>` |
| `QuestGiverRequestItems` | QuestPackets.cs | `List<QuestObjectiveCollect>` |
| `QuestGiverOfferRewardMessage` | QuestPackets.cs | Multiple reward lists |
| `QuestGiverQuestComplete` | QuestPackets.cs | Optional reward display |
| `DisplayToast` | QuestPackets.cs | Multiple switch-based writes |
| `AuraUpdate` | SpellPackets.cs | `List<AuraInfo> Auras` |
| `SpellStart` | SpellPackets.cs | Complex spell cast data |
| `SpellGo` | SpellPackets.cs | Complex spell cast data |
| `SpellPeriodicAuraLog` | SpellPackets.cs | `List<SpellLogEffect>` |
| `FeatureSystemStatus` | SystemPackets.cs | Complex with many optional fields |
| `FeatureSystemStatusGlueScreen` | SystemPackets.cs | Complex with version-dependent lists |
| `TradeUpdated` | TradePackets.cs | `List<TradeItem>` |
| `UpdateObject` | UpdatePackets.cs | Complex object update data |

</details>

<details>
<summary>Complex Authentication / Session Packets</summary>

| Packet | File | Reason |
|--------|------|--------|
| `AuthResponse` | AuthenticationPackets.cs | Complex with optional sections |
| `ConnectTo` | AuthenticationPackets.cs | Connection data with addresses |
| `EnterEncryptedMode` | AuthenticationPackets.cs | Encryption keys |
| `BattlenetNotification` | SessionPackets.cs | Bnet protocol data |
| `BattlenetResponse` | SessionPackets.cs | Bnet protocol data |
| `ChangeRealmTicketResponse` | SessionPackets.cs | Auth ticket data |

</details>

<details>
<summary>Unbounded Strings / Other</summary>

| Packet | File | Reason |
|--------|------|--------|
| `PartyInvite` | GroupPackets.cs | Multiple unbounded strings (realm names, etc.) |

</details>

## MaxSize Optimizations

Based on `spanstats.log` analysis, several packets were allocating far more memory than needed. These have been optimized with reduced caps that still use fallback to `Write()` for rare oversized cases.

| Packet | Old MaxSize | New MaxSize | Reduction |
|--------|-------------|-------------|-----------|
| `ContactList` | 36,605 | ~2,933 | **92%** |
| `UpdateAccountData` | 16,419 | ~2,083 | **87%** |
| `AllAccountCriteria` | 13,060 | ~1,636 | **87%** |
| `ChatPkt` | 8,511 | ~1,087 | **87%** |
| `SendKnownSpells` | 4,617 | ~1,097 | **76%** |
| `SetupCurrency` | 3,844 | ~484 | **87%** |
| `SetAllTaskProgress` | 3,332 | ~420 | **87%** |
| `LoadCUFProfiles` | 2,048 | 256 | **88%** |
| `MOTD` | 2,065 | ~517 | **75%** |
| `MonsterMove` | 1,134 | ~366 | **68%** |
| `LFGListUpdateBlacklist` | 1,028 | ~132 | **87%** |
| `ChannelNotifyJoined` | 357 | ~165 | **54%** |
| `MoveUpdate` | 256 | 192 | **25%** |
