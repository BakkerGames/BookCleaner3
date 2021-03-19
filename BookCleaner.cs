using System;
using System.Collections;
using System.IO;
using System.Text;

namespace BookCleaner3
{
    public static class BookCleaner
    {
        public static bool FixEbook(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new SystemException($"File not found: {filename}");
            }
            bool changed = false;
            bool hangingQuote = false;
            string[] lines = File.ReadAllLines(filename);
            StringBuilder result = new();
            foreach (string line in lines)
            {
                string tempLine = line;
                if (tempLine.StartsWith("\t###"))
                {
                    changed = true;
                    continue;
                }
                if (hangingQuote)
                {
                    if (!tempLine.StartsWith("\t\"") &&
                        !tempLine.StartsWith("\t<i>\"") &&
                        !tempLine.StartsWith("\t\\\""))
                    {
                        result.AppendLine("\t### hanging quote mismatch ###");
                        changed = true;
                    }
                    hangingQuote = false;
                }
                changed = changed || FixSpaces(ref tempLine);
                if (FindQuoteErrors(tempLine, ref hangingQuote))
                {
                    result.AppendLine("\t### mismatched quotes ###");
                    changed = true;
                }
                if (FindTagErrors(tempLine))
                {
                    result.AppendLine("\t### mismatched tags ###");
                    changed = true;
                }
                result.AppendLine(tempLine);
            }
            if (changed)
            {
                File.WriteAllText(filename, result.ToString());
            }
            return changed;
        }

        private static bool FindQuoteErrors(string tempLine, ref bool hangingQuote)
        {
            char lastChar = ' ';
            bool inQuote = false;
            bool firstChar = true;
            bool inTag = false;
            hangingQuote = false;
            if (tempLine.StartsWith("\t\t")) return false;
            if (tempLine.StartsWith("\t<table>")) return false;
            if (tempLine.StartsWith("\t<th>")) return false;
            if (tempLine.StartsWith("\t<tr>")) return false;
            if (tempLine.StartsWith("\t<td>")) return false;
            tempLine = tempLine.Replace("\" '", "\"'");
            tempLine = tempLine.Replace("' \"", "'\"");
            tempLine = tempLine.Replace(" —", "—");
            tempLine = tempLine.Replace("— ", "—");
            foreach (char c in tempLine)
            {
                if (inTag)
                {
                    if (c == '>')
                    {
                        inTag = false;
                    }
                    continue;
                }
                if (c == '<')
                {
                    inTag = true;
                    continue;
                }
                if (firstChar)
                {
                    if (c == '\t') continue;
                    if (c == ' ') continue;
                    if (c == '^') continue;
                    if (c == '|') continue;
                    if (c == ']') continue;
                    firstChar = false;
                }
                if (c == '"')
                {
                    if (lastChar == '\\') // ignore escaped quotes
                    {
                        lastChar = ' ';
                        continue;
                    }
                    if (!inQuote)
                    {
                        if (lastChar != ' ' &&
                            lastChar != '—' &&
                            lastChar != ';' &&
                            lastChar != '\'' &&
                            lastChar != '(' &&
                            lastChar != '[')
                        {
                            return true; // error
                        }
                        inQuote = true;
                        lastChar = ' ';
                        continue;
                    }
                }
                else if (lastChar == '"')
                {
                    if (c != ' ' &&
                        c != '—' &&
                        c != '.' &&
                        c != '!' &&
                        c != '?' &&
                        c != ',' &&
                        c != ';' &&
                        c != ':' &&
                        c != '&' &&
                        c != '\'' &&
                        c != ')' &&
                        c != ']')
                    {
                        return true; // error
                    }
                    inQuote = false;
                }
                lastChar = c;
            }
            if (inQuote && lastChar != '"')
            {
                hangingQuote = true;
            }
            return false;
        }

        private static bool FixSpaces(ref string tempLine)
        {
            string orig = tempLine;
            if (tempLine.EndsWith(" "))
                tempLine = tempLine.TrimEnd();
            while (tempLine.Contains("\t "))
                tempLine = tempLine.Replace("\t ", "\t");
            while (tempLine.Contains("  "))
                tempLine = tempLine.Replace("  ", " ");
            return (orig != tempLine);
        }

        private static bool FindTagErrors(string tempLine)
        {
            if (!tempLine.Contains("<") && !tempLine.Contains(">"))
            {
                return false;
            }
            Stack tagStack = new();
            int posBegin = 0;
            int posEnd = 0;
            while (tempLine.IndexOf("<", posBegin) >= 0)
            {
                posBegin = tempLine.IndexOf("<", posBegin);
                posEnd = tempLine.IndexOf(">", posBegin);
                if (posEnd < 0)
                {
                    return true; // not a tag
                }
                string tag = tempLine[(posBegin + 1)..posEnd];
                posBegin = posEnd;
                // ignore various tags
                if (tag.StartsWith("image")) continue;
                if (tag.StartsWith("outdent")) continue;
                // table tags may span lines
                if (tag.StartsWith("table")) continue;
                if (tag == "/table") continue;
                if (tag.StartsWith("th")) continue;
                if (tag == "/th") continue;
                if (tag.StartsWith("tr")) continue;
                if (tag == "/tr") continue;
                if (tag.StartsWith("td")) continue;
                if (tag == "/td") continue;
                if (tag.StartsWith("/")) // closing tag
                {
                    tag = tag[1..];
                    if (tagStack.Count == 0)
                    {
                        return true; // ending without beginning
                    }
                    if (tag != tagStack.Pop().ToString())
                    {
                        return true; // tags don't match
                    }
                }
                else if (tag.EndsWith("/"))
                {
                    // self-closing tag, do nothing
                }
                else
                {
                    if (tag.StartsWith("a "))
                    {
                        tag = "a"; // ignore rest of parameters
                    }
                    tagStack.Push(tag);
                }
            }
            if (tagStack.Count > 0)
            {
                return true;
            }
            return false;
        }
    }
}
