using Domain.Dtos;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Application
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

        public static void UpdateTask(this INinja ninja, Guid taskId, double progressPercent, DateTime expectedEndTime, string message = null)
        {
            ninja.UpdateTask(new TaskStateDto
            {
                TaskId = taskId,
                ProgressPercent = progressPercent,
                ExpectedEndTime = expectedEndTime,
                Message = message
            });
        }
    }
}
