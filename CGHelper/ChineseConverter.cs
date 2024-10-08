﻿using System;
using System.Runtime.InteropServices;

namespace CommonLibrary
{
    /// <summary>
    /// C#使用LCMapString API函數進行中文簡繁體互相轉換
    /// </summary>
    public class ChineseConverter
    {
        internal const int LOCALE_SYSTEM_DEFAULT = 0x0800;
        internal const int LCMAP_SIMPLIFIED_CHINESE = 0x02000000;
        internal const int LCMAP_TRADITIONAL_CHINESE = 0x04000000;

        /// <summary>
        /// 使用OS的kernel.dll做為簡繁轉換工具，只要有裝OS就可以使用，不用額外引用dll，但只能做逐字轉換，無法進行詞意的轉換
        /// <para>所以無法將電腦轉成計算機</para>
        /// </summary>
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int LCMapString(int Locale, int dwMapFlags, string lpSrcStr, int cchSrc, [Out] string lpDestStr, int cchDest);

        /// <summary>
        /// 繁體轉簡體
        /// </summary>
        /// <param name="pSource">要轉換的繁體字：體</param>
        /// <returns>轉換後的簡體字：體</returns>
        public static string ToSimplified(string pSource)
        {
            String tTarget = new String(' ', pSource.Length);
            int tReturn = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_SIMPLIFIED_CHINESE, pSource, pSource.Length, tTarget, pSource.Length);
            return tTarget;
        }
        
        /// <summary>
        /// 簡體轉繁體
        /// </summary>
        /// <param name="pSource">要轉換的繁體字：體</param>
        /// <returns>轉換後的簡體字：體</returns>
        public static string ToTraditional(string pSource)
        {
            String tTarget = new String(' ', pSource.Length);
            int tReturn = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_TRADITIONAL_CHINESE, pSource, pSource.Length, tTarget, pSource.Length);
            return tTarget;
        }
    }
}
