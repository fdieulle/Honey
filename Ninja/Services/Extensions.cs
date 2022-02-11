using Microsoft.Extensions.Logging;
using Ninja.Dto;
using System;
using System.IO;

namespace Ninja.Services
{
    public static class Extensions
    {
        #region Folder tools

        public static string CreateFolder(this string path, ILogger logger = null)
        {
            if (string.IsNullOrEmpty(path)) return path;

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Can't create the folder: {0}", path);
            }

            return path;
        }

        public static void DeleteFolder(this string path, ILogger logger = null)
        {
            try
            {
                if (path.FolderExists())
                    Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Can't delete the folder: {0}", path);
            }
        }

        public static bool FolderExists(this string path)
            => !string.IsNullOrEmpty(path) && Directory.Exists(path);

        #endregion

        public static bool IsFinal(this JobState state)
        {
            switch (state)
            {
                case JobState.Pending:
                case JobState.Running:
                    return false;
                case JobState.Done:
                case JobState.Cancel:
                case JobState.Error:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
