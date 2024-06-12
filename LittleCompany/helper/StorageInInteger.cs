using System;

namespace LittleCompany.helper
{
    internal class StorageInInteger
    {
        static public int GetIntFromFourBytes(byte pFirst, byte pSecond, byte pThird, byte pFourth)
        {
            return (pFourth << 24) + (pThird << 16) + (pSecond << 8) + pFirst;
        }

        static public byte[] GetFourBytesFromInt(int pDataSource) {
            return BitConverter.GetBytes(pDataSource); ;
        }
    }
}