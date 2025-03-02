using LogExpert.Classes.Filter;
using LogExpert.Entities;

using System;
using System.Collections.Generic;
using System.Drawing;

namespace LogExpert.Config
{
    [Serializable]
    public class Settings
    {
        #region Fields

        public bool alwaysOnTop;

        public Rectangle appBounds;

        public Rectangle appBoundsFullscreen;

        public IList<ColumnizerHistoryEntry> columnizerHistoryList = [];

        public List<ColorEntry> fileColors = [];

        public List<string> fileHistoryList = [];

        public List<string> filterHistoryList = [];

        public List<FilterParams> filterList = [];

        public FilterParams filterParams = new();

        public List<string> filterRangeHistoryList = [];

        public bool hideLineColumn;

        public bool isMaximized;

        public string lastDirectory;

        public List<string> lastOpenFilesList = [];

        public Preferences Preferences { get; set; } = new();

        public RegexHistory regexHistory = new();

        public List<string> searchHistoryList = [];

        public SearchParams searchParams = new();

        public IList<string> uriHistoryList = [];

        public int versionBuild;

        #endregion
    }
}