#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;

public class FixPathInBuildGradle : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => int.MaxValue;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
#if UNITY_EDITOR_WIN
        var buildGradlePath = Path.Combine(path.Replace(@"\", @"/"), "build.gradle");
        if (File.Exists(buildGradlePath))
        {
            var contents = File.ReadAllText(buildGradlePath);
            var correctContents = contents.Replace(@"\Assets", @"/Assets");
            
            File.WriteAllText(buildGradlePath, correctContents);
        }
#endif
    }
}
#endif