using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GSParser.Editor.Core
{
    [InitializeOnLoad]
    public static class GitignoreModifier
    {
        private static readonly string[] LinesToAdd = new[]
        {
            "",
            "# --- GSParser Config Ignore ---",
            "/Assets/Editor/[Cc]onfig.meta",
            "*.cfg",
            "*.cfg.meta"
        };

        static GitignoreModifier()
        {
            ModifyRootGitignore();
        }

        public static void ModifyRootGitignore()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string gitignorePath = Path.Combine(projectRoot, ".gitignore");

            if (!File.Exists(gitignorePath))
                return;

            try
            {
                string content = File.ReadAllText(gitignorePath);

                if (content.Contains("GSParser Config Ignore"))
                    return;

                StringBuilder sb = new StringBuilder(content);
                foreach (var line in LinesToAdd)
                    sb.AppendLine(line);

                File.WriteAllText(gitignorePath, sb.ToString());
            }
            catch (System.Exception e) { }
        }
    }
}
