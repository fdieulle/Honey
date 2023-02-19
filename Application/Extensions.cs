using Domain.Dtos;
using log4net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Application
{
    public static class Extensions
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Folder tools

        public static string CreateFolder(this string path, ILogger logger = null)
        {
            if (string.IsNullOrEmpty(path)) return path;

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception e) { Logger.Error($"Can't create the folder: {path}", e); }

            return path;
        }

        public static void DeleteFolder(this string path, ILogger logger = null)
        {
            try
            {
                if (path.FolderExists())
                    Directory.Delete(path, true);
            }
            catch (Exception e) { Logger.Error($"Can't delete the folder: {path}", e); }
        }

        public static bool FolderExists(this string path)
            => !string.IsNullOrEmpty(path) && Directory.Exists(path);

        #endregion

        public static void UpdateTask(this IFlower flower, Guid taskId, double progressPercent, DateTime expectedEndTime, string message = null)
        {
            flower.UpdateTask(new TaskStateDto
            {
                TaskId = taskId,
                ProgressPercent = progressPercent,
                ExpectedEndTime = expectedEndTime,
                Message = message
            });
        }

        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> x, TKey key, Func<TKey, TValue> factory)
        {
            if (!x.TryGetValue(key, out var value))
                x.Add(key, value = factory(key));
            return value;
        }
    }
}
