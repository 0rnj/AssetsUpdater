using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeBase
{
    public static class StringExtensions
    {
        public static string LimitByteLength(this string input, int limitByte)
        {
            return new string(input
                .TakeWhile((c, i) =>
                    Encoding.UTF8.GetByteCount(input.Substring(0, i + 1)) <= limitByte)
                .ToArray());
        }
        
        public static string ToSnakeCase(this string text)
        {
            if(text.Length < 2) {
                return text;
            }
            
            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(text[0]));
            
            for(var i = 1; i < text.Length; ++i) {
                var c = text[i];
                
                if (char.IsUpper(c))
                {
                    sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else {
                    sb.Append(c);
                }
            }
            
            return sb.ToString();
        }
        
        public static string SnakeToPascalCase(this string text)
        {
            if(text.Length < 2) {
                return text;
            }
            
            var sb = new StringBuilder();
            sb.Append(char.ToUpperInvariant(text[0]));
            var setNextUppercase = false;
            
            for (var i = 1; i < text.Length; ++i)
            {
                var c = text[i];

                if (c == '_')
                {
                    setNextUppercase = true;
                    continue;
                }
                
                if (setNextUppercase && char.IsLower(c))
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
                
                setNextUppercase = false;
            }
            
            return sb.ToString();
        }
        
        public static T Convert<T>(this string input)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter.ConvertFromString(input);
            }
            catch (NotSupportedException)
            {
                return default;
            }
        }
        
        public static string PascalToLabel(this string pascalString)
        {
            return Regex.Replace(pascalString, "(\\B[A-Z])", " $1");
        }

        public static string WithNextSentence(this string sentence, string nextSentence)
        {
            var sb = new StringBuilder();

            sb.Append(sentence)
                .Append(sentence.EndsWith(".") ? " " : ". ")
                .Append(nextSentence);

            return sb.ToString();
        }

        public static StringBuilder Append(this string str1, string str2)
        {
            var sb = new StringBuilder();
            sb.Append(str1);
            sb.Append(str2);
            return sb;
        }

        public static string NormalizeRelativePath(this string path)
        {
            return string.IsNullOrEmpty(path)
                ? path
                : path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string NormalizeFullPath(this string path)
        {
            return string.IsNullOrEmpty(path) 
                ? path 
                : Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static void NormalizeRelativePaths(this IList<string> paths)
        {
            for (var i = 0; i < paths.Count; i++)
            {
                paths[i] = paths[i].NormalizeRelativePath();
            }
        }

        public static void NormalizeFullPaths(this IList<string> paths)
        {
            for (var i = 0; i < paths.Count; i++)
            {
                paths[i] = paths[i].NormalizeFullPath();
            }
        }
        
        public static string ReplaceWithEmpty(this string source, string target)
        {
            return source.Replace(target, string.Empty);
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNotNullOrEmpty(this string str)
        {
            return str.IsNullOrEmpty() == false;
        }
    }
}