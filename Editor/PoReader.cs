using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sonic853.TotpGen
{
    public class PoReader
    {
        public string language
        {
            get
            {
                return this.m_language;
            }
        }

        public string lastTranslator
        {
            get
            {
                return this.m_lastTranslator;
            }
        }

        public PoReader()
        {
        }
        public PoReader(string path)
        {
            this.path = path;
            this.ReadPoFile(path);
        }
        public bool ReadPoFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { return false; }
            this.path = path;
            var lines = File.ReadAllLines(path);
            var msgidIndex = -1;
            var msgstrIndex = -1;
            msgids.Clear();
            msgstrs.Clear();
            m_language = "";
            m_lastTranslator = "";
            foreach (var line in lines)
            {
                if (line.StartsWith("msgid \""))
                {
                    var text = line.Substring(7, line.LastIndexOf('"') - 7);
                    msgids.Add(Decode(text, 0, text.Length));
                    msgidIndex = msgids.Count - 1;
                    msgstrIndex = -1;
                }
                else if (line.StartsWith("msgstr \""))
                {
                    var text = line.Substring(8, line.LastIndexOf('"') - 8);
                    
                    msgstrs.Add(Decode(text, 0, text.Length));
                    msgstrIndex = msgstrs.Count - 1;
                    msgidIndex = -1;
                }
                // 找到符合"Language: 的行，然后获取语言，同时去除后面的换行
                else if (line.StartsWith("\"Language: ") && msgstrIndex == 0)
                {
                    m_language = line.Substring(11, line.LastIndexOf('"') - 11);
                    // 找到并去除\n
                    m_language = m_language.IndexOf("\\n") == -1 ? m_language : m_language.Substring(0, m_language.LastIndexOf("\\n"));
                }
                // 找到符合"Last-Translator: 的行，然后获取语言，同时去除后面的换行
                else if (line.StartsWith("\"Last-Translator: ") && msgstrIndex == 0)
                {
                    m_lastTranslator = line.Substring(20, line.LastIndexOf('"') - 20);
                    // 找到并去除\n
                    m_lastTranslator = m_lastTranslator.IndexOf("\\n") == -1 ? m_lastTranslator : m_lastTranslator.Substring(0, m_lastTranslator.LastIndexOf("\\n"));
                    // 将<和>替换为＜和＞
                    m_lastTranslator = m_lastTranslator.Replace("<", "＜").Replace(">", "＞");
                }
                else if (line.StartsWith("\""))
                {
                    if (msgidIndex > 0)
                    {
                        var text = line.Substring(1, line.LastIndexOf('"') - 1);
                        msgids[msgidIndex] += Decode(text, 0, text.Length);
                    }
                    else if (msgstrIndex > 0)
                    {
                        var text = line.Substring(1, line.LastIndexOf('"') - 1);
                        msgstrs[msgstrIndex] += Decode(text, 0, text.Length);
                    }
                }
            }
            return true;
        }
        string Decode(string source, int startIndex, int count, string newLine = "\n")
        {
            var builder = new StringBuilder();
            for (var endIndex = startIndex + count; startIndex < endIndex; startIndex++)
            {
                var c = source[startIndex];
                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (++startIndex < endIndex)
                {
                    c = source[startIndex];
                    switch (c)
                    {
                        case '\\':
                        case '"':
                            builder.Append(c);
                            continue;
                        case 't':
                            builder.Append('\t');
                            continue;
                        case 'r':
                            var index = startIndex;
                            if (++index + 1 < endIndex && source[index] == '\\' && source[++index] == 'n')
                                startIndex = index;
                            // "\r" and "\r\n" are both accepted as new line
                            builder.Append(newLine);
                            continue;
                        case 'n':
                            builder.Append(newLine);
                            continue;
                    }
                }

                // invalid escape sequence
                return builder.ToString();
            }

            return builder.ToString();
        }

        public string _(string text)
        {
            return this._getText(text);
        }

        public string _getText(string text)
        {
            var index = msgids.IndexOf(text);
            if (index == -1)
            {
                return text;
            }
            return msgstrs[index];
        }

        private string path = "";

        private List<string> msgids = new List<string>();

        private List<string> msgstrs = new List<string>();

        private string m_language = "";

        private string m_lastTranslator = "";
    }
}
