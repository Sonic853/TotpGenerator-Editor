using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

namespace Sonic853.TotpGen
{
    public class PoReader
    {
        private string path = "";
        private List<string> msgids = new List<string>();
        private List<string> msgstrs = new List<string>();
        private string m_language = "";
        public string language
        {
            get
            {
                return m_language;
            }
        }
        private string m_lastTranslator = "";
        public string lastTranslator
        {
            get
            {
                return m_lastTranslator;
            }
        }
        public PoReader() { }
        public PoReader(string path)
        {
            this.path = path;
            ReadPoFile(path);
        }
        public bool ReadPoFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }
            this.path = path;
            string[] lines = File.ReadAllLines(path);
            int msgidIndex = -1;
            int msgstrIndex = -1;
            msgids.Clear();
            msgstrs.Clear();
            m_language = "";
            m_lastTranslator = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("msgid \""))
                {
                    msgids.Add(returnText(line.Substring(7, line.LastIndexOf('"') - 7)));
                    msgidIndex = msgids.Count - 1;
                    msgstrIndex = -1;
                }
                else if (line.StartsWith("msgstr \""))
                {
                    msgstrs.Add(returnText(line.Substring(8, line.LastIndexOf('"') - 8)));
                    msgstrIndex = msgstrs.Count - 1;
                    msgidIndex = -1;
                }
                // else if (line.StartsWith("msgstr[0] \""))
                // {
                //     msgstrs.Add(returnText(line.Substring(11, line.LastIndexOf('"') - 11)));
                //     msgstrIndex = msgstrs.Count - 1;
                //     msgidIndex = -1;
                // }
                else if (line.StartsWith("\"Language: ") && msgstrIndex == 0)
                {
                    m_language = line.Substring(11, line.LastIndexOf('"') - 11);
                    // 找到并去除\n
                    m_language = m_language.IndexOf("\\n") == -1 ? m_language : m_language.Substring(0, m_language.LastIndexOf("\\n"));
                }
                else if (line.StartsWith("\"Last-Translator: ") && msgstrIndex == 0)
                {
                    m_lastTranslator = line.Substring(20, line.LastIndexOf('"') - 20);
                    // 找到并去除\n
                    m_lastTranslator = m_lastTranslator.IndexOf("\\n") == -1 ? m_lastTranslator : m_lastTranslator.Substring(0, m_lastTranslator.LastIndexOf("\\n"));
                    // 将<和>替换为＜和＞
                    // m_lastTranslator = m_lastTranslator.Replace("<", "＜").Replace(">", "＞");
                }
                else if (line.StartsWith("\""))
                {
                    if (msgidIndex != -1 && msgidIndex != 0)
                    {
                        msgids[msgidIndex] += returnText(line.Substring(1, line.LastIndexOf('"') - 1));
                    }
                    else if (msgstrIndex != -1 && msgstrIndex != 0)
                    {
                        msgstrs[msgstrIndex] += returnText(line.Substring(1, line.LastIndexOf('"') - 1));
                    }
                }
            }
            return true;
        }
        string returnText(string text)
        {
            // if (text.EndsWith("\""))
            // {
            //     text = text.Substring(0, text.Length - 1);
            // }
            if (text.EndsWith("\\\\n"))
            {
                text = text.Substring(0, text.Length - 3);
                text = text + "\\n";
            }
            else if (text.EndsWith("\\n"))
            {
                text = text.Substring(0, text.Length - 2);
                text = text + "\n";
            }
            return text;
        }
        public string _(string text)
        {
            return _getText(text);
        }
        public string _getText(string text)
        {
            int index = msgids.IndexOf(text);
            if (index == -1)
            {
                return text;
            }
            return msgstrs[index];
        }
    }
}