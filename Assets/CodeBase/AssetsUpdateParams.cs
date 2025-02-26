#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace CodeBase
{
    public class AssetsUpdateParams<T> : AssetsUpdateParamsBase
    {
        public readonly IReadOnlyList<string> FilePaths;
        public readonly Func<T, bool> Action;
        public readonly Func<Context<T>, bool> DetailedAction;

        public string[] AllowedFileExtensions { get; }
        public string[] IgnoredFilepathSubstrings { get; }
        public Type[] IgnoredFieldTypes { get; }
        public int MaxAllowedNesting { get; }

        public AssetsUpdateParams(IReadOnlyList<string> filePaths, Func<T, bool> action)
        {
            FilePaths = filePaths;
            Action = action;
            AllowedFileExtensions = DefaultAllowedFileExtensions;
            IgnoredFilepathSubstrings = DefaultIgnoredFilepathSubstrings;
            IgnoredFieldTypes = DefaultIgnoredFieldTypes;
            MaxAllowedNesting = DefaultMaxAllowedNesting;
        }

        public AssetsUpdateParams(IReadOnlyList<string> filePaths, Func<Context<T>, bool> detailedAction)
        {
            FilePaths = filePaths;
            DetailedAction = detailedAction;
            AllowedFileExtensions = DefaultAllowedFileExtensions;
            IgnoredFilepathSubstrings = DefaultIgnoredFilepathSubstrings;
            IgnoredFieldTypes = DefaultIgnoredFieldTypes;
            MaxAllowedNesting = DefaultMaxAllowedNesting;
        }
    }
}
#endif