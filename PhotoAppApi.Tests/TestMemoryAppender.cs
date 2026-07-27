using log4net.Appender;
using log4net.Core;
using System.Collections.Generic;

namespace PhotoAppApi.Tests
{
    public class TestMemoryAppender : MemoryAppender
    {
        public new IReadOnlyList<LoggingEvent> GetEvents()
        {
            return m_eventsList.ToArray();
        }
    }
}
