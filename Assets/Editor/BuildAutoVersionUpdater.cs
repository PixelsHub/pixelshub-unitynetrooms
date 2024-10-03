#if UNITY_EDITOR
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEditor.PixelsHub
{
    public class BuildAutoVersionUpdater : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var version = PlayerSettings.bundleVersion.Split('.');

            string last = version[^1];

            Regex regex = new(@"\d+$");
            Match match = regex.Match(last);

            if(match.Success)
            {
                int lastVersionNumber = int.Parse(match.Value);
                string restOfString = last.Substring(0, match.Index);

                StringBuilder sb = new(version[0]);
                for(int i = 1; i < version.Length - 1; i++)
                    sb.Append('.').Append(version[i]);

                sb.Append('.').Append(restOfString).Append(++lastVersionNumber);
                
                var newVersion = sb.ToString();

                string message = $"Current app version is {PlayerSettings.bundleVersion}.\nDo you wish to automatically update to {newVersion}?";

                if(EditorUtility.DisplayDialog("App version", message, "Yes", "No"))
                {
                    PlayerSettings.bundleVersion = newVersion;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("App version", $"Your app version is {PlayerSettings.bundleVersion}.\nThis code might be invalid.", "Ok");
                return;
            }
        }
    }
}
#endif