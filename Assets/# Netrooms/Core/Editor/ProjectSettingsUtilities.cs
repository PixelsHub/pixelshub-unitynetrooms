#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.Build;

namespace UnityEditor.PixelsHub
{
    public static class ProjectSettingsUtilities
    {
        public static bool AddDefineSymbol(string symbol, NamedBuildTarget target)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbols(target);
            if(!symbols.Contains(symbol))
            {
                symbols = string.IsNullOrEmpty(symbols) ? symbol : symbols + ";" + symbol;
                PlayerSettings.SetScriptingDefineSymbols(target, symbols);

                return true;
            }

            return false;
        }

        public static bool RemoveDefineSymbol(string symbol, NamedBuildTarget target)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbols(target);
            if(symbols.Contains(symbol))
            {
                string[] symbolsArray = symbols.Split(';');

                for(int i = 0; i < symbolsArray.Length; i++)
                {
                    if(symbolsArray[i] == symbol)
                        symbolsArray[i] = string.Empty;
                }

                symbols = string.Join(";", symbolsArray);
                PlayerSettings.SetScriptingDefineSymbols(target, symbols);

                return true;
            }

            return false;
        }
    }
}
#endif