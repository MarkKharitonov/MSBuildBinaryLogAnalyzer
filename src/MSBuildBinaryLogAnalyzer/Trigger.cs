using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSBuildBinaryLogAnalyzer
{
    internal class Trigger
    {
        public readonly string ProjectName;
        public readonly List<string> ItemsToHash;
        public List<(string FirstBuild, string SecondBuild)> Diff;

        public Trigger(string projectName, List<string> itemsToHash)
        {
            ProjectName = projectName;
            ItemsToHash = itemsToHash;
        }

        public void DiffItemsToHash(List<string> itemsToHash)
        {
            Diff = new List<(string FirstBuild, string SecondBuild)>();
            ItemsToHash.RemoveAll(itemsToHash.Remove);

            var count = Math.Min(ItemsToHash.Count, itemsToHash.Count);
            int i = 0;
            for (; i < count; ++i)
            {
                if (ItemsToHash[i] != itemsToHash[i])
                {
                    int prevPos, pos = -1;
                    do
                    {
                        prevPos = pos;
                        pos = ItemsToHash[i].IndexOf('\\', pos + 1);
                    } while (pos > 0 && string.Compare(ItemsToHash[i], 0, itemsToHash[i], 0, pos) == 0);

                    Diff.Add((FirstBuild: itemsToHash[i].Substring(prevPos), SecondBuild: ItemsToHash[i].Substring(prevPos)));
                }
            }
            for (; i < ItemsToHash.Count; ++i)
            {
                Diff.Add((FirstBuild: null, SecondBuild: ItemsToHash[i]));
            }
            for (; i < itemsToHash.Count; ++i)
            {
                Diff.Add((FirstBuild: itemsToHash[i], SecondBuild: null));
            }
        }

        public override string ToString()
        {
            if (Diff == null || Diff.Count == 0)
            {
                return ProjectName;
            }

            return new StringBuilder(ProjectName)
                .Append(" (")
                .Append(string.Join(" , ", Diff.Select(d => $"...{d.FirstBuild} vs ...{d.SecondBuild}")))
                .Append(")")
                .ToString();
        }
    }
}