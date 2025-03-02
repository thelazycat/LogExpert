using LogExpert.Classes.Highlight;

using System;
using System.Collections.Generic;

namespace LogExpert.Entities
{
    [Serializable]
    public class HighlightGroup : ICloneable
    {
        #region Properties

        public string GroupName { get; set; } = string.Empty;

        public List<HighlightEntry> HighlightEntryList { get; set; } = [];

        public object Clone()
        {
            HighlightGroup clone = new()
            {
                GroupName = GroupName
            };

            foreach (HighlightEntry entry in HighlightEntryList)
            {
                clone.HighlightEntryList.Add((HighlightEntry)entry.Clone());
            }

            return clone;
        }

        #endregion
    }
}