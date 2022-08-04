// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem
{
    /// <summary>
    /// Abstraction of FileSystem APIs, this allows unit tests to execute without touching hard drive.
    /// But also allows host to not store anything on physical file system, instead everything can be
    /// kept in memory and discarded after running.
    /// </summary>
    public interface IPhysicalFileSystem
    {
        /// <summary>
        /// Same behavior as <see cref="Directory.Exists(string)"/>.
        /// </summary>
        bool DirectoryExists(string directory);

        /// <summary>
        /// Same behavior as <see cref="File.Exists(string)"/>.
        /// </summary>
        bool FileExists(string file);

        /// <summary>
        /// Same behavior as <see cref="File.Create(string)"/>.
        /// </summary>
        Stream CreateFile(string path);

        /// <summary>
        /// Same behavior as <see cref="Directory.CreateDirectory(string)"/>.
        /// </summary>
        void CreateDirectory(string path);

        /// <summary>
        /// Same behavior as <see cref="Directory.GetCurrentDirectory()"/>.
        /// </summary>
        string GetCurrentDirectory();

        /// <summary>
        /// Same behavior as <see cref="Directory.EnumerateFileSystemEntries(string, string, SearchOption)"/>.
        /// </summary>
        IEnumerable<string> EnumerateFileSystemEntries(string directoryName, string pattern, SearchOption searchOption);

        /// <summary>
        /// Same behavior as <see cref="File.Copy(string, string, bool)"/>.
        /// </summary>
        void FileCopy(string sourcePath, string targetPath, bool overwrite);

        /// <summary>
        /// Same behavior as <see cref="Directory.Delete(string, bool)"/>.
        /// </summary>
        void DirectoryDelete(string path, bool recursive);

        /// <summary>
        /// Same behavior as <see cref="File.ReadAllText(string)"/>.
        /// </summary>
        string ReadAllText(string path);

        /// <summary>
        /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
        /// Same behavior as <see cref="File.ReadAllBytes(string)"/>.
        /// </summary>
        byte[] ReadAllBytes(string path);

        /// <summary>
        /// Same behavior as <see cref="File.WriteAllText(string, string)"/>.
        /// </summary>
        void WriteAllText(string path, string value);

        /// <summary>
        /// Same behavior as <see cref="Directory.EnumerateDirectories(string, string, SearchOption)"/>.
        /// </summary>
        IEnumerable<string> EnumerateDirectories(string path, string pattern, SearchOption searchOption);

        /// <summary>
        /// Same behavior as <see cref="Directory.EnumerateFiles(string, string, SearchOption)"/>.
        /// </summary>
        IEnumerable<string> EnumerateFiles(string path, string pattern, SearchOption searchOption);

        /// <summary>
        /// Same behavior as <see cref="File.OpenRead(string)"/>.
        /// </summary>
        Stream OpenRead(string path);

        /// <summary>
        /// Same behavior as <see cref="File.Delete(string)"/>.
        /// </summary>
        void FileDelete(string path);

        /// <summary>
        /// Same behavior as <see cref="File.GetAttributes(string)"/>.
        /// </summary>
        FileAttributes GetFileAttributes(string file);

        /// <summary>
        /// Same behavior as <see cref="File.SetAttributes(string, FileAttributes)"/>.
        /// </summary>
        void SetFileAttributes(string file, FileAttributes attributes);

        /// <summary>
        /// Same behavior as <see cref="FileSystemWatcher"/>.
        /// </summary>
        /// <remarks>
        /// Creates new <see cref="FileSystemWatcher"/> which monitors specified path and on any changes calls <paramref name="fileChanged"/> callback.
        /// To stop watching dispose returned object.
        /// </remarks>
        IDisposable WatchFileChanges(string filepath, FileSystemEventHandler fileChanged);

        /// <summary>
        /// Gets the last write time for the <paramref name="file"/> in UTC.
        /// Same behavior as <see cref="File.GetLastWriteTimeUtc(string)"/>.
        /// </summary>
        /// <param name="file">The file to get last write time for.</param>
        /// <returns></returns>
        DateTime GetLastWriteTimeUtc(string file);

        /// <summary>
        /// Sets the last write time for the <paramref name="file"/> in UTC.
        /// Same behavior as <see cref="File.SetLastWriteTimeUtc(string, DateTime)"/>.
        /// </summary>
        /// <param name="file">The file to set last write time for.</param>
        /// <param name="lastWriteTimeUtc">the time to set.</param>
        void SetLastWriteTimeUtc(string file, DateTime lastWriteTimeUtc);

        /// <summary>
        /// If target is a subpath of relativeTo a relative bath from relativeTo to subpath will be returned.
        /// </summary>
        /// <param name="target">Path to be converted to relative if possible.</param>
        /// <param name="relativeTo">Base of the relative path to be returned.</param>
        /// <returns></returns>
        string PathRelativeTo(string target, string relativeTo);
    }
}
