using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Data.Configuration;
using PugTilemap;
using UnityEngine;

namespace QuickToolSwap
{
    public class ConfigReader
    {
        private readonly ConfigFile _configFile;
        private readonly List<Section> _sections;

        public ConfigReader(ConfigFile configFile)
        {
            _configFile = configFile;
            SetDefaults();
            _sections = Section.FromConfigFile(_configFile);
        }

        public List<string> GetPriorityList(List<string> foundElements)
        {
            foreach (var section in _sections)
            {
                foreach (var foundElement in foundElements)
                {
                    if (section.ActiveOn.Any(x => x == foundElement.ToLower()))
                        return section.PriorityList;
                }
            }

            return new List<string>();
        }

        private void SetDefaults()
        {
            var sections = new[]
            {
                GetMiningSection(),
                GetFarmingSection(),
                GetCatchingSection(),
                GetFishingSection(),
                GetDiggingSection()
            };
            foreach (var section in sections)
                CreateDefaultSettings(section);
        }

        private void CreateDefaultSettings(Section section)
        {
            _configFile.Bind(
                section.Name,
                "activeOn",
                ToCommaSeparatedString(section.ActiveOn),
                ""
            );
            _configFile.Bind(
                section.Name,
                "priorityList",
                ToCommaSeparatedString(section.PriorityList),
                ""
            );
            return;

            string ToCommaSeparatedString(List<string> list) => string.Join(", ", list.Select(t => t.ToString()));
        }


        private class Section
        {
            public readonly string Name;
            public readonly List<string> ActiveOn;
            public readonly List<string> PriorityList;

            public Section(string name, List<string> activeOn, List<string> priorityList)
            {
                Name = name;
                ActiveOn = activeOn;
                PriorityList = priorityList;
            }

            public static List<Section> FromConfigFile(ConfigFile configFile)
            {
                var bindedEntries = configFile.Entries.ToDictionary(x => x.Key, x => x.Value.BoxedValue.ToString());
                var orphanEntries = configFile.OrphanedEntries;
                var entries = bindedEntries.Concat(orphanEntries).ToList();

                var sectionNames = entries
                    .GroupBy(x => x.Key.Section)
                    .Select(x => x.Key)
                    .ToList();

                return sectionNames.Select(sectionName => GetSection(entries, sectionName)).ToList();
            }

            private static Section GetSection(List<KeyValuePair<ConfigDefinition, string>> entries, string sectionName)
            {
                var activeOnEntry =
                    entries.FirstOrDefault(x => x.Key.Section == sectionName && x.Key.Key == "activeOn");
                var priorityListEntry =
                    entries.FirstOrDefault(x => x.Key.Section == sectionName && x.Key.Key == "priorityList");

                if (activeOnEntry.Value == null || priorityListEntry.Value == null)
                {
                    Debug.LogError("activeOn or priorityList not found");
                    return null;
                }

                var activeOn = ToList(activeOnEntry.Value);
                var priorityList = ToList(priorityListEntry.Value);

                return new Section(sectionName, activeOn, priorityList);
            }

            private static List<string> ToList(string commaSeparatedString)
            {
                return commaSeparatedString
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToLower())
                    .Where(x => x != "")
                    .ToList();
            }
        }

        private static Section GetMiningSection()
        {
            return new Section
            (
                "Mining",
                new List<object>
                {
                    TileType.ore,
                    TileType.wall
                }.Select(x => x.ToString()).ToList(),
                new List<ObjectID>
                {
                    ObjectID.LightningGun,
                    ObjectID.LaserDrillTool,
                    ObjectID.LegendaryMiningPick,
                    ObjectID.SolariteMiningPick,
                    ObjectID.GalaxiteMiningPick,
                    ObjectID.OctarineMiningPick,
                    ObjectID.AncientMiningPick,
                    ObjectID.DrillToolScarlet,
                    ObjectID.ScarletMiningPick,
                    ObjectID.IronMiningPick,
                    ObjectID.DrillTool,
                    ObjectID.TinMiningPick,
                    ObjectID.CopperMiningPick,
                    ObjectID.WoodMiningPick
                }.Select(x => x.ToString()).ToList()
            );
        }

        private static Section GetFarmingSection()
        {
            return new Section
            (
                "Farming",
                new List<object>
                {
                    ObjectID.HeartBerryPlant,
                    ObjectID.GlowingTulipPlant,
                    ObjectID.BombPepperPlant,
                    ObjectID.CarrockPlant,
                    ObjectID.PuffungiPlant,
                    ObjectID.RootPlant,
                    ObjectID.GrubKapokPlant,
                    ObjectID.CoralRootPlant,
                    ObjectID.BloatOatPlant,
                    ObjectID.PewpayaPlant,
                    ObjectID.PinegrapplePlant,
                    ObjectID.GrumpkinPlant,
                    ObjectID.SunricePlant,
                    ObjectID.LunacornPlant,
                    ObjectID.GleamRootPlant,
                    TileType.wateredGround,
                    TileType.groundSlime
                }.Select(x => x.ToString()).ToList(),
                new List<ObjectID>
                {
                    ObjectID.ScarletHoe,
                    ObjectID.IronHoe,
                    ObjectID.TinHoe,
                    ObjectID.CopperHoe,
                    ObjectID.WoodHoe
                }.Select(x => x.ToString()).ToList()
            );
        }

        private static Section GetCatchingSection()
        {
            return new Section
            (
                "Catching",
                new List<object>
                {
                    ObjectID.CritterBeetle,
                    ObjectID.CritterLarva,
                    ObjectID.CritterCrab,
                    ObjectID.ButterflySunset,
                    ObjectID.ButterflyDreamy,
                    ObjectID.ButterflyCitrus,
                    ObjectID.ButterflyIcy,
                    ObjectID.ButterflyBase,
                    ObjectID.CritterScorpion,
                    ObjectID.CritterGrasshopper,
                    ObjectID.CritterWorm,
                    ObjectID.CritterCentipede,
                    ObjectID.CritterCockroach,
                    ObjectID.CritterCrab2,
                    ObjectID.CritterTinySnail,
                    ObjectID.CritterSnootFly,
                    ObjectID.CritterNewt,
                    ObjectID.CritterPassageFly
                }.Select(x => x.ToString()).ToList(),
                new List<ObjectID>
                {
                    ObjectID.BugNet
                }.Select(x => x.ToString()).ToList()
            );
        }

        private static Section GetFishingSection()
        {
            return new Section
            (
                "Fishing",
                new List<object>
                {
                    TileType.water
                }.Select(x => x.ToString()).ToList(),
                new List<ObjectID>
                {
                    ObjectID.SolariteFishingRod,
                    ObjectID.GalaxiteFishingRod,
                    ObjectID.OctarineFishingRod,
                    ObjectID.ScarletFishingRod,
                    ObjectID.IronFishingRod,
                    ObjectID.TinFishingRod,
                    ObjectID.WoodFishingRod
                }.Select(x => x.ToString()).ToList()
            );
        }

        private static Section GetDiggingSection()
        {
            return new Section
            (
                "Digging",
                new List<object>
                {
                    ObjectID.DiggingSpot,
                    ObjectID.DiggingSpotNature,
                    ObjectID.DiggingSpotSea,
                    ObjectID.DiggingSpotDesert,
                    ObjectID.DiggingSpotLava
                }.Select(x => x.ToString()).ToList(),
                new List<ObjectID>
                {
                    ObjectID.GalaxiteShovel,
                    ObjectID.OctarineShovel,
                    ObjectID.ScarletShovel,
                    ObjectID.IronShovel,
                    ObjectID.TinShovel,
                    ObjectID.CopperShovel,
                    ObjectID.WoodShovel
                }.Select(x => x.ToString()).ToList()
            );
        }
    }
}
