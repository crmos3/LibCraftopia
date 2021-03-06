﻿using HarmonyLib;
using Oc;
using Oc.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using LibCraftopia.Enchant;
using System.Text;
using UnityEngine;

namespace LibCraftopia.Helper
{
    public class EnchantHelper : SingletonMonoBehaviour<EnchantHelper>
    {

        public static readonly int maxRarity = 5;//SoEnchantDataList.MaxRarity i don't now how to access

        protected override void OnUnityAwake()
        {
            UnspecifiedEnemyDrop = new Dictionary<int, float>();
            SpecifiedEnemyDrop = new Dictionary<int, Dictionary<int, float>>();
            TreeRandomDrop = new Dictionary<int, float>();
            StoneRandomDrop = new Dictionary<int, float>();

            soResidentDataTraverse = new Traverse(OcResidentData.Inst);
            enchantList = soResidentDataTraverse.Field<SoEnchantDataList>("_enchantDataList").Value;
            enchantListTraverse = new Traverse(enchantList);
            vanillaLastID = LastId(AllEnchant);
        }

        private Traverse soResidentDataTraverse;
        private SoEnchantDataList enchantList;
        private Traverse enchantListTraverse;
        private SoEnchantment[] allEnchantCache;
        private int lastId;
        internal int vanillaLastID;

        //The number of enchantments an item or enemy can hold is limited, so hold them separately.
        public Dictionary<int, float> UnspecifiedEnemyDrop { get; private set; }
        public Dictionary<int, Dictionary<int, float>> SpecifiedEnemyDrop { get; private set; }
        public Dictionary<int, float> TreeRandomDrop { get; private set; }
        public Dictionary<int, float> StoneRandomDrop { get; private set; }

        private SoEnchantment[] cachedAllEnchant()
        {
            var all = new Traverse(enchantList).Field<SoEnchantment[]>("all").Value;
            if (all != allEnchantCache)
            {
                allEnchantCache = all;
                lastId = LastId(all);
            }
            return all;
        }

        public SoEnchantment[] AllEnchant
        {
            get { return cachedAllEnchant(); }
        }

        public SoEnchantment[] ValidEnchant
        {
            get {
                return (from enchant in AllEnchant where enchant.IsEnabled select enchant).ToArray();
            }
        }

        public int NewId()
        {
            cachedAllEnchant();
            return ++lastId;
        }

        private int LastId(SoEnchantment[] all)
        {
            return Math.Max(lastId, all.Select(enchant => enchant.ID).Max());
        }

        public void AddEnchant(params EnchantSetting[] settings)
        {
            var newAllEnchant = allEnchantCache.Concat(TakeOut(settings)).ToArray();
            enchantListTraverse.Field<SoEnchantment[]>("all").Value = newAllEnchant;
            UpdateTreassureProb(settings);
            UpdateStoneProb(settings);
            UpdateTreeProb(settings);
            UpdateUnspecifiedEnemyProb(settings);
            UpdateSpecifiedEnemyProb(settings);
        }

        private int VanillaLastId(SoEnchantment[] all)
        {
            return AllEnchant.Select(enchant => enchant.ID).Max();
        }

        private SoEnchantment[] TakeOut(EnchantSetting[] settings)
        {
            return (from s in settings select s.enchant).ToArray<SoEnchantment>();
        }

        private void UpdateTreassureProb(EnchantSetting[] settings)
        {
            var maxId = settings.Select(setting => setting.enchant.ID).Max();
            var rarityChestProbSums = enchantListTraverse.Field<float[]>("rarityChestProbSums").Value;

            for (int i = 0; i < maxRarity; i++) {
                var field = new Traverse(enchantList).Field<float[]>(string.Format("rarity{0}ChestProbs", i + 1));
                field.Value = field.Value.Concat(new float[maxId - vanillaLastID]).ToArray();
                var probs = field.Value;
                float sum = 0;
                foreach(var s in settings)
                {
                    probs[s.enchant.ID] = s.probInTreassureBoxes[i];
                    sum += s.probInTreassureBoxes[i];
                }
                rarityChestProbSums[i] += sum;
            }

        }

        private void UpdateStoneProb(EnchantSetting[] settings)
        {
            foreach (var s in settings)
            {
                StoneRandomDrop[s.AssignedID] = s.probInStone;
            }
        }

        private void UpdateTreeProb(EnchantSetting[] settings)
        {
            foreach (var s in settings)
            {
                TreeRandomDrop[s.AssignedID] = s.probInTree;
            }
        }

        private void UpdateUnspecifiedEnemyProb(EnchantSetting[] settings)
        {
            foreach (var s in settings)
            {
                UnspecifiedEnemyDrop[s.AssignedID] = s.probInRandomDrop;
            }
        }

        private void UpdateSpecifiedEnemyProb(EnchantSetting[] settings)
        {
            foreach (var s in settings)
            {
                for(int i = 0; i < s.targetEnemyId.Length; i++)
                {
                    var targetId = s.targetEnemyId[i];
                    var prob = s.probsInEnemyDrop[i];
                    if (!SpecifiedEnemyDrop.ContainsKey(targetId))
                    {
                        SpecifiedEnemyDrop[targetId] = new Dictionary<int, float>();
                    }
                    SpecifiedEnemyDrop[targetId][s.AssignedID] = prob;
                }
            }
        }
    }
}
