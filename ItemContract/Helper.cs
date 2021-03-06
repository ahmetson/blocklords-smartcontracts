using Neo.SmartContract.Framework.Services.Neo;
using System.Numerics;

namespace LordsContract
{
    public static class Helper
    {
        public static void ChangeItemOwner(byte[] itemId, BigInteger heroId)
        {
            string key = GeneralContract.ITEM_MAP + itemId;
            byte[] bytes = Storage.Get(Storage.CurrentContext, key);

            if (bytes.Length <= 0)
            {
                Runtime.Notify(1005);
                throw new System.Exception();
            }

            Item item = (Item)Neo.SmartContract.Framework.Helper.Deserialize(bytes);
            if (item.BATCH != GeneralContract.HERO_CREATION_BATCH)
            {
                Runtime.Notify(4012);
                throw new System.Exception();
            }

            item.HERO = heroId;
            item.BATCH = GeneralContract.NO_BATCH;
            bytes = Neo.SmartContract.Framework.Helper.Serialize(item);

            //Put.Item(itemId, item, true);
            Storage.Put(Storage.CurrentContext, key, bytes);
        }

        public static byte[] GetIdByIndex(byte[][] arr, BigInteger arrLength, BigInteger index)
        {
            BigInteger passedIndex = 0;
            for (int i = 0; i < arrLength; i++, passedIndex = passedIndex + 1)
            {
                if (index == passedIndex)
                {
                    return arr[i];
                }
            }

            return new byte[0];
        }

        public static BigInteger GetCoffer(BigInteger cityId)
        {
            BigInteger[] coffers = GetCoffers();

            int index = (int)cityId;
            return coffers[index];
        }

        public static BigInteger[] GetCoffers()
        {
            byte[] coffersBytes = Storage.Get(GeneralContract.CITY_COFFERS_KEY);
            if (coffersBytes.Length <= 0)
            {
                Runtime.Notify(14);
                throw new System.Exception();
            }

            BigInteger[] coffers = (BigInteger[])Neo.SmartContract.Framework.Helper.Deserialize(coffersBytes);

            return coffers;
        }

        public static void SetCoffer(BigInteger cityId, BigInteger cofferSize)
        {
            BigInteger[] coffers = GetCoffers();

            int index = (int)cityId;
            coffers[index] = cofferSize;

            SetCoffers(coffers);
            
        }

        public static void SetCoffers(BigInteger[] coffers)
        {
            byte[] coffersBytes = Neo.SmartContract.Framework.Helper.Serialize(coffers);
            Storage.Put(GeneralContract.CITY_COFFERS_KEY, coffersBytes);
        }
    }
}


