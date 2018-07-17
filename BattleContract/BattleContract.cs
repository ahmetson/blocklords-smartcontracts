using BattleContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

/**
 *  Battle of the Blocklords game.
 *  
 *  Version: 1.0
 *  Author: Medet Ahmetson
 *
 */

namespace Blocklords
{
    class BattleContract : SmartContract
    {
        private static readonly string itemContract = "123131313123123123";
        private static readonly string heroContract = "454545454545454545";

        private static readonly int XP_1 = 500, XP_2 = 100;

        private static readonly int ATTACKER_WON = 0,
            DEFENDER_WON = 1, BOTH_LOOSE = 2;

        private static readonly int idLength = 13, itemIdLength = 13;

        private static readonly int leadershipIndex     = 0;
        private static readonly int strengthIndex       = 4;
        private static readonly int speedIndex          = 8;
        private static readonly int intelligenceIndex   = 12;
        private static readonly int defenceStatIndex    = 16;
        private static readonly int nationIndex         = 20;
        private static readonly int classIndex          = 21;
        private static readonly int addressIndex        = 23;
        private static readonly int optionalDataIndex = 22;

        private static readonly int statLength          = 4;
        private static readonly int nationLength        = 1;
        private static readonly int classLength         = 1;
        private static readonly int optionalDataLength = 1;
        private static readonly int addressLength       = 33;
        private static readonly int heroParametersLength = addressIndex + addressLength;

        private static readonly int itemStatValueIndex = 0,
            itemStatMaxValueIndex = 4, itemStatTypeIndex = 5, itemQualityIndex = 6, itemOwnerIndex = 7,
            itemStatValueLength = 4, itemStatMaxValueLength = 4, itemStatTypeLength = 1, itemQualityLength = 1, itemOwnerLength = 33;
        private static readonly int itemParametersLength = itemOwnerIndex + itemOwnerLength;



        private static byte[] GetFalseByte()
        {
            return new BigInteger(0).AsByteArray();
        }
        private static byte[] GetTrueByte()
        {
            return new BigInteger(1).AsByteArray();
        }

        public static byte[] Main(string operation, object[] args)
        {

            /*if (operation == "put" || 
                  operation == "update" ||
                  operation == "setItemState" || 
                  operation == "transfer" )
            {
                if (!Runtime.CheckWitness((byte[])args[0]))
                {
                    Runtime.Log("Authorization failed!");
                    return GetFalseByte();
                }
            }*/
            // @Param Owner Address, Attacker ID, Attacker Items List, Defender Address, Defender ID, Defender Items List
            if (operation == "attackHero")
            {
                Hero attacker, defender;

                if (!args.Length.Equals(17)) return GetFalseByte();
                if (!IsValidHeroId((string)args[0])) return GetFalseByte();
                if (!IsValidHeroId((string)args[7])) return GetFalseByte();

                attacker = ParameterToHero((string)args[0], (string)args[14]);
                if (attacker == null) return GetFalseByte();
                attacker.troops = (int)args[1];
                string[] attackerItemIds = new string[]
                {
                    (string)args[2],
                    (string)args[3],
                    (string)args[4],
                    (string)args[5],
                    (string)args[6]
                };

                attacker.items = ParameterToItems(attackerItemIds, (string)args[14]);
                if (attacker.items == null) return GetFalseByte();
                attacker.isNPC = false;

                defender = ParameterToHero((string)args[7], (string)args[15]);
                if (defender == null) return GetFalseByte();
                defender.troops = (int)args[8];

                string[] defenderItemIds = new string[]
                {
                    (string)args[9],
                    (string)args[10],
                    (string)args[11],
                    (string)args[12],
                    (string)args[13]
                };

                defender.items = ParameterToItems(defenderItemIds, (string)args[15]);
                if (defender.items == null) return GetFalseByte();
                defender.isNPC = SetNPCMode((int)args[17]);

                /*if (!Runtime.CheckWitness(((string)args[14]).AsByteArray()))
                {
                    Runtime.Log("Authorization failed!");
                    return GetFalseByte();
                }*/

                return AttackHero(attacker, defender);
            }

            // @Param Hero ID
            if (operation == "attackCity") return AttackCity((string)args[0]);

            return GetFalseByte();
        }

        private static byte[] AttackCity(string itemId)
        {
            // Validate input
            if (!IsValidHeroId(itemId))
            {
                Runtime.Log("Invalid Item ID parameter!");
                return GetFalseByte();
            }
            byte[] item = Storage.Get(Storage.CurrentContext, itemId);
            Runtime.Log("Item is " + item.AsString());
            return item;
        }

        /* Battle Calculation for Hero VS. Hero: 
         * Involved in the Battle, the heroes, the number of troops, the items from both part
         * 
         * Passing Arguments
         * Hero #1 ID (13)              ARG #1
         * //Hero #1 Speed Stat (4)       
         * //Hero #1 Strength Stat (4)
         * Hero #1 Troops Number (4)    ARG #2
         * Hero #1 Item 1 Id (13)       ARG #3
         * Hero #1 Item 2 Id (13)       ARG #4
         * Hero #1 Item 3 Id (13)       ARG #5
         * Hero #1 Item 4 Id (13)       ARG #6
         * Hero #1 Item 5 Id (13)       ARG #7 
         * Hero #1 Address              ARG #15
         * Hero #2 ID (13)              ARG #8
         * //Hero #2 Speed Stat (4)
         * //Hero #2 Strength Stat (4)
         * //Hero #2 Defence Stat (4)
         * Hero #2 Troops Number (4)    ARG #9
         * Hero #2 Item 1 Id (13)       ARG #10
         * Hero #2 Item 2 Id (13)       ARG #11
         * Hero #2 Item 3 Id (13)       ARG #12
         * Hero #2 Item 4 Id (13)       ARG #13
         * Hero #2 Item 5 Id (13)       ARG #14
         * Hero #2 NPC (1)              ARG #16
         * Hero #2 Address              ARG #17
         * 
         * 
         * Steps of the Battle:
         * 1) Calculate the Damage
         * 2) Attack to both of heroes
         * 3) Check who win or both lose
         * 4) If non of them win nor lose, repeat the step 1)
         * 5) If someone win the battle, go to step 6)
         * 6) Decide how many item stats will increase
         * 7) Calculate the increase amount
         * 8) Increase the stats
         * 
         * TOTAL LENGTH OF PASSING ARGUMENTS ARE
         * 
         * Involved Attacker's Speed, Strength, HP, Troops number
         * Involved Defenser's Speed, Strength, HP, Troops number and Defense
         * 
         */

        private static int CalculateDamage(Hero hero, bool defender = false, bool npc = false)
        {
            return 1;
        }
        private static int AcceptDamage(int damage, int troops)
        {
            int remainedTroops = 1;

            return remainedTroops;
        }
        /**
         * Return Result:
         * 0 - no one win or lose
         * 1 - win first hero
         * 2 - win second hero
         * 3 - both of the heroes lose
         */
        private static int DecideDamageEffect(int aTroops, int aRemained, int dTroops, int dRemained)
        {
            return BattleContract.BOTH_LOOSE;
        }

        private static byte[] AttackHero(Hero attacker, Hero defender)
        {
            int attackerDamage = CalculateDamage(attacker, false, false);
            int defenderDamage = CalculateDamage(defender, true, defender.isNPC);

            int defenderRemainTroops = AcceptDamage(attackerDamage, defender.troops);
            int attackerRemainTroops = AcceptDamage(defenderDamage, attacker.troops);

            int battleResult = DecideDamageEffect(attacker.troops, attackerRemainTroops, defender.troops, defenderRemainTroops);
            if (battleResult.Equals(BattleContract.ATTACKER_WON))
            {
                return RewardWinner(attacker);
            }
            if (battleResult.Equals(BattleContract.DEFENDER_WON))
            {
                return RewardWinner(defender);
            }

            //Storage.Put(Storage.CurrentContext, heroId, heroParams);
            //Runtime.Notify(new object[] { "Address has returned from the blockchain storage" });
            return GetFalseByte();
        }

        private static byte[] RewardWinner(Hero hero)
        {
            int increaseItemAmount = DecideIncreasingItemAmount(hero.items);
            int[] randomItemIndexes = GetRandomNumbers(0, hero.items.Length - 1, increaseItemAmount);

            for(int i=0; i<increaseItemAmount; i++)
            {
                int increaseValue = GetIncreaseValue(hero.items[i]);
                IncreaseStat(hero.items[i], increaseValue);
            }

            return GetFalseByte();
        }
        private static int DecideIncreasingItemAmount(Item[] heroItems)
        {
            return 0;
        }
        private static int[] GetRandomNumbers(int min, int max, int amount)
        {
            int[] numbers = new int[amount];
            return numbers;
        }
        private static int GetIncreaseValue(Item item)
        {
            return 0;
        }
        private static void IncreaseStat(Item item, int increaseValue)
        {

        }
        private static void LogBattleResult()
        {

        }

        /**
         *  Checks the item id. Item ID's length should be exactly 15.
         *  First 13 is represents the Unix timestamp in Milliseconds.
         *  And last 2 digits are representing the random number, just for case.
         */
        private static bool IsValidHeroId(string itemId)
        {
            return itemId.Length.Equals(BattleContract.idLength);
        }

        // 
        private static Item ParameterToItem(string itemId, string player)
        {
            StorageContext storageContext = new StorageContext();
            string parameter = Storage.Get(storageContext, itemId.AsByteArray()).AsString();
            if (!parameter.Length.Equals(BattleContract.itemParametersLength))
            {
                return null;
            }
            if (!parameter.EndsWith(player))
            {
                return null;
            }

            Item item = new Item();

            item.id = itemId;
            item.statType;
            return item;
        }
        private static Item[] ParameterToItems(string[] ids, string itemOwner)
        {
            Item[] items;
            int itemsNum = 5;
            // Decide the Length
            for (int i=1; i<ids.Length; i++)
            {
                if (ids[i].Length.Equals(itemIdLength))
                {
                    itemsNum = i + 1;
                }
            }
            items = new Item[itemsNum];
            for (int i=0; i<itemsNum; i++)
            {
                Item item = ParameterToItem(ids[i], itemOwner);
                if (item == null)
                {
                    return null;
                } else
                {
                    items[i] = item;
                }
            }

            return items;
        }

        private static Hero ParameterToHero(string heroId, string address)
        {
            StorageContext storageContext = new StorageContext();
            string parameter = Storage.Get(storageContext, heroId.AsByteArray()).AsString();
            if (!parameter.Length.Equals(BattleContract.heroParametersLength))
            {
                return null;
            }
            if (!parameter.EndsWith(address))
            {
                return null;
            }

            Hero hero = new Hero
            {
                id = heroId,
                leadership = new BigInteger(parameter.Substring(leadershipIndex, statLength).AsByteArray()),
                strength = new BigInteger(parameter.Substring(strengthIndex, statLength).AsByteArray()),
                speed = new BigInteger(parameter.Substring(speedIndex, statLength).AsByteArray()),
                intelligence = new BigInteger(parameter.Substring(intelligenceIndex, statLength).AsByteArray()),
                defense = new BigInteger(parameter.Substring(defenceStatIndex, statLength).AsByteArray())
            };

            return hero;
        }

        private static bool SetNPCMode(int npcInt)
        {
            if (npcInt.Equals(0))
            {
                return false;
            }
            return true;
        }
    }
}
