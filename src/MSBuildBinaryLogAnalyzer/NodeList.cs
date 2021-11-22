using System.Collections.Generic;

namespace MSBuildBinaryLogAnalyzer
{
    internal class NodeList<T>
    {
        private readonly List<T> m_data = new();

        public T this[int nodeId]
        {
            get => nodeId >= m_data.Count ? default : m_data[nodeId];
            set
            {
                while (nodeId >= m_data.Count)
                {
                    m_data.Add(default);
                }

                m_data[nodeId] = value;
            }
        }
    }
}