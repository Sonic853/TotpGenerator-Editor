using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

namespace Sonic853.TotpGen
{
    public class MainWindow : EditorWindow
    {
        private static readonly string path = Path.Combine("Packages", "com.sonic853.totpgenerator");
        public static PoReader poReader;
        static MainWindow instance;
        public static MainWindow getInstance
        {
            get
            {
                return instance;
            }
        }
        public static MainWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    OpenMainWindow();
                }
                return instance;
            }
        }
        public static readonly string OpenWindowCommand = nameof(OpenMainWindowCommand);
        [MenuItem("Window/853Lab/Totp Generator", false, 3010)]
        public static void OpenMainWindowCommand()
        {
            if (CommandService.Exists(OpenWindowCommand))
                CommandService.Execute(OpenWindowCommand, CommandHint.Menu);
            else
            {
                OpenMainWindow();
            }
        }
        public static void OpenMainWindow()
        {
            instance = GetWindow<MainWindow>();
            instance.minSize = new Vector2(200, 100);
            instance.titleContent = new GUIContent(_("Totp 生成器"));
        }
        public void OnEnable()
        {
            poReader = new PoReader(Path.Combine(path, "Editor", "Language", $"{EditorPrefs.GetString("Editor.kEditorLocale", "ChineseSimplified")}.po"));
            VisualElement root = rootVisualElement;
            ScrollView scrollView = new ScrollView();
            scrollView.AddToClassList("main");
            root.Add(scrollView);
            scrollView.Add(TotpGenerator.CreateUI());
        }
        void OnGUI()
        {
            TotpGenerator.OnGUI();
        }
        void OnDestroy()
        {
            TotpGenerator.OnDestroy();
        }
        static string _(string text)
        {
            return poReader._(text);
        }
    }
}
