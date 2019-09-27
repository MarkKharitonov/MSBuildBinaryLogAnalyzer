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
            if (ItemsToHash.Count != itemsToHash.Count)
            {
                return;
            }

            Diff = new List<(string FirstBuild, string SecondBuild)>();
            for (var i = 0; i < ItemsToHash.Count; ++i)
            {
                if (ItemsToHash[i] != itemsToHash[i])
                {
                    int prevPos,  pos = -1;
                    do
                    {
                        prevPos = pos;
                        pos = ItemsToHash[i].IndexOf('\\', pos + 1);
                    } while (string.Compare(ItemsToHash[i], 0, itemsToHash[i], 0, pos) == 0);

                    Diff.Add((FirstBuild: itemsToHash[i].Substring(prevPos), SecondBuild: ItemsToHash[i].Substring(prevPos)));
                }
            }
        }

        public override string ToString()
        {
            if (Diff?.Count == 0)
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