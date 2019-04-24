using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SolidProxy.Core.Configuration.Builder
{
    /// <summary>
    /// The signature matcher is used ot create signatures for the global, assembly, type or method scopes
    /// </summary>
    public class SignatureMatcher
    {
        /// <summary>
        /// Returns the assembly signature where the supplied method belongs.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public string CreateAssemblySignature(MethodBase methodInfo)
        {
            var sb = new StringBuilder();
            CreateAssemblySignature(sb, methodInfo);
            return sb.ToString();
        }

        /// <summary>
        /// Returns the type signature where the supplied method belongs.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public string CreateTypeSignature(MethodBase methodInfo)
        {
            var sb = new StringBuilder();
            CreateAssemblySignature(sb, methodInfo);
            CreateTypeSignature(sb, methodInfo);
            return sb.ToString();
        }
        /// <summary>
        /// Returns the type signature where the supplied method belongs.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public string CreateMethodSignature(MethodBase methodInfo)
        {
            var sb = new StringBuilder();
            CreateAssemblySignature(sb, methodInfo);
            CreateTypeSignature(sb, methodInfo);
            CreateMethodSignature(sb, methodInfo);
            return sb.ToString();
        }

        /// <summary>
        /// Returns true if supplied patterns matches the assembly that the method resides in.
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public bool AssemblyMatches(MethodBase methodBase, params string[] patterns)
        {
            return PatternsMatches(CreateAssemblySignature(methodBase), patterns);
        }

        /// <summary>
        /// Returns true if supplied patterns matches the assembly that the method resides in.
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public bool TypeMatches(MethodBase methodBase, params string[] patterns)
        {
            return PatternsMatches(CreateTypeSignature(methodBase), patterns);
        }

        /// <summary>
        /// Returns true if supplied patterns matches the assembly that the method resides in.
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public bool MethodMatches(MethodBase methodBase, params string[] patterns)
        {
            return PatternsMatches(CreateMethodSignature(methodBase), patterns);
        }

        private bool PatternsMatches(string template, string[] patterns)
        {
            foreach(var pattern in patterns)
            {
                if (!pattern.StartsWith("["))
                {
                    throw new Exception("Pattern does not start with [");
                }
                if (!pattern.StartsWith("["))
                {
                    throw new Exception("Pattern does not end with ]");
                }
                var convPattern = pattern.Substring(1, pattern.Length - 2);
                convPattern = convPattern.Replace("+", "\\+");
                convPattern = convPattern.Replace("*", "[^\\]]+");
                convPattern = $"\\[{convPattern}\\]";
                var regex = new Regex(convPattern);
                if(!regex.IsMatch(template))
                {
                    return false;
                }
            }
            return true;
        }

        private void CreateAssemblySignature(StringBuilder sb, MethodBase methodInfo)
        {
            sb.Append("[").Append("Assembly.FullName:").Append(methodInfo.DeclaringType.Assembly.FullName).AppendLine("]");
        }

        private void CreateTypeSignature(StringBuilder sb, MethodBase methodInfo)
        {
            sb.Append("[").Append("Type.FullName:").Append(methodInfo.DeclaringType.FullName).AppendLine("]");
        }

        private void CreateMethodSignature(StringBuilder sb, MethodBase methodInfo)
        {
            sb.Append("[").Append("Method.Name:").Append(methodInfo.Name).AppendLine("]");
            foreach(var attr in methodInfo.CustomAttributes)
            {
                sb.Append("[").Append("Method.Attribute:").Append(attr.AttributeType.FullName).AppendLine("]");
            }
        }
    }
}
