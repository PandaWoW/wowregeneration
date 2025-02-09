﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using WoWRegeneration.Repositories;

namespace WoWRegeneration.Data
{
    public class ManifestFile
    {
        private const string LocaleDetectLineVersion2 = "serverpath=locale_";
        private const string LocaleDetectLineVersion3 = "tag=";
        private readonly string[] LocaleDetectLineVersion3IgnoredTags = { "base", "OSX", "Win", "ALT", "EXP1", "EXP2", "EXP3", "EXP4",  "enTW", "enCN" };

        private int Version { get; set; }

        private List<string> Lines { get; } = new List<string>();

        public List<string> GetLocales()
        {
            List<string> tmp = new List<string>();

            if (Version == 2)
                tmp = (from line in Lines where line.StartsWith(LocaleDetectLineVersion2) select line.Replace(LocaleDetectLineVersion2, "")).ToList();

            if (Version == 3)
            {
                foreach (string line in Lines)
                {
                    if (line.StartsWith(LocaleDetectLineVersion3))
                    {
                        string tagValue = line.Replace(LocaleDetectLineVersion3, "");

                        if (!LocaleDetectLineVersion3IgnoredTags.Contains(tagValue) && !tmp.Contains(tagValue))
                            tmp.Add(tagValue);
                    }
                }
            }

            tmp.Add("All");
            return tmp;
        }

        public List<FileObject> GenerateFileList()
        {
            WoWRepository repository = RepositoriesManager.GetRepositoryByMfil(WoWRegeneration.CurrentSession.MFil);

            HashSet<FileObject> tmp = new HashSet<FileObject>();

            foreach (string line in Lines)
            {
                if (IsLineARepositorFile(repository, line))
                {
                    var file = new FileObject { Path = GetFilePath(line) };
                    if (file.Path == null)
                        continue;
                    file.Url = line;
                    file.Info = GetFileInfo(repository, line);
                    file.Size = GetFileSize(repository, line);
                    if (IsAcceptedFile(repository, file))
                        tmp.Add(file);
                }
            }

            return tmp.ToList();
        }

        private bool IsAcceptedFile(WoWRepository repository, FileObject file)
        {
            if (WoWRegeneration.CurrentSession.Os == "Win" && file.Filename == "base-OSX.MPQ")
                return false;

            if (WoWRegeneration.CurrentSession.Os == "OSX" && file.Filename == "base-Win.MPQ")
                return false;

            if (WoWRegeneration.CurrentSession.CompletedFiles.Contains(file.Path) && File.Exists(Program.ExecutionPath + repository.GetDefaultDirectory() + file.Path))
            {
                Program.Log("Skipping " + file.Filename + " allready downloaded", ConsoleColor.DarkGray);
                return false;
            }

            if (WoWRegeneration.CurrentSession.CompletedFiles.Contains(file.Path))
            {
                WoWRegeneration.CurrentSession.CompletedFiles.Remove(file.Path);
                WoWRegeneration.CurrentSession.SaveSession();
            }

            if (file.Directory == "Data/")
                return true;

            if (file.Directory.StartsWith("Data/Interface/"))
                return true;

            if (file.Directory.StartsWith("Data/" + WoWRegeneration.CurrentSession.Locale))
                return true;

            if (WoWRegeneration.CurrentSession.Locale == "All" && file.Directory.Length == 10)
                return true;

            return false;
        }

        private string GetFilePath(string line)
        {
            int index = Lines.IndexOf(line);
            if (Lines[index + 1].StartsWith("name="))
            {
                return Lines[index + 1].Replace("name=", "");
            }
            return null;
        }

        private string GetFileInfo(WoWRepository repository, string line)
        {
            int index = Lines.IndexOf(line);
            for (int n = 1; n <= 5; n++)
            {
                string next = Lines[index + n];
                if (IsLineARepositorFile(repository, next))
                    return null;
                if (next.StartsWith("path=locale_"))
                    return next.Replace("path=locale_", "");
            }
            return null;
        }

        private long GetFileSize(WoWRepository repository, string line)
        {
            int index = Lines.IndexOf(line);
            for (int n = 1; n <= 5; n++)
            {
                string next = Lines[index + n];
                if (IsLineARepositorFile(repository, next))
                    return 0;
                if (next.StartsWith("path=locale_"))
                    return 0;
                if (next.StartsWith("size="))
                    return long.Parse(next.Replace("size=", ""));
            }
            return 0;
        }

        private bool IsLineARepositorFile(WoWRepository repository, string line)
        {
            if (line.StartsWith(repository.GetBaseUrl()))
            {
                if (line.Substring(line.Length - 4, 1) == ".")
                {
                    return true;
                }
            }
            return false;
        }

        public static ManifestFile FromRepository(WoWRepository repository)
        {
            try
            {
                var manifest = new ManifestFile();
                var client = new WebClient();

                string content = client.DownloadString(repository.GetBaseUrl() + repository.GetMFilName());

                string[] lines = content.Split('\n');

                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("version="))
                    {
                        int output;
                        if (int.TryParse(line.Trim().Replace("version=", ""), out output))
                        {
                            manifest.Version = output;
                        }
                    }
                    manifest.Lines.Add(line.Trim().Replace("file=", repository.GetBaseUrl()));
                }

                return manifest;
            }
            catch (Exception ex)
            {
                Program.Log("Unable to retrieve Manifest file", ConsoleColor.Red);
                Program.Log(ex.Message, ConsoleColor.Red);
            }
            return null;
        }
    }
}
