using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

/**
 *  Market Contract of the Blocklords game.
 *  
 *  Version: 1.0
 *  Author: Medet Ahmetson
 *   
 */
namespace Blocklords
{
    public class MerchantContract : SmartContract
    {
        public static event Action<byte[], byte[], BigInteger> TransferEvent;

        private static readonly int itemStateIndex = 0;
        private static readonly int addressIndex = 9;
        private static readonly int addressLength = 34;
        private static readonly int itemParamsLength = 44;
        private static readonly int itemIdLength = 15;

        private static byte[] GetFalseByte()
        {
            byte[] falseByte = new BigInteger(0).AsByteArray();

            //Runtime.Notify(new object[] { "return", falseByte });
            return falseByte;
        }

        private static byte[] GetTrueByte()
        {
            byte[] trueByte = new BigInteger(1).AsByteArray();

            //Runtime.Notify(new object[] { "return", trueByte });
            return trueByte;
        }

        public static byte[] Main(string operation, string address, string arg1)
        {

            // @Param Owner Address, Item ID
            if (operation == "put") return Put(address, arg1);

            // Not available in this Version of the Game. Since Item can be equiped by the Hero. Equiped items
            // are not tradable. And, in the Smart Contract we can't check it.
            // @Param Item ID, From Address, To Address
            //if (operation == "buy")            return Buy(arg1, arg2, arg3);   

            return GetFalseByte();
        }

        private static byte[] Put(string address, string itemId)
        {
            if (!Runtime.CheckWitness(address.AsByteArray()))
            {
                Runtime.Log("Authorization failed!");
                return GetFalseByte();
            }
            // Validate input
            if (IsValidItemId(itemId) == 0)
            {
                Runtime.Log("Invalid Item ID parameter!");
                return GetFalseByte();
            }
            byte[] item = Storage.Get(Storage.CurrentContext, itemId);
            Runtime.Log("Item is " + item.AsString());
            return item;
        }


        /**
         *  Checks the item id. Item ID's length should be exactly 15.
         *  First 13 is represents the Unix timestamp in Milliseconds.
         *  And last 2 digits are representing the random number, just for case.
         */
        private static int IsValidItemId(string itemId)
        {
            if (itemId.Length == MerchantContract.itemIdLength)
                return 1;
            return 0;
        }

    }
}
