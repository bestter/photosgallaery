using log4net;
using System.Reflection;

namespace PhotoAppApi
{
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Error(string message, Exception? ex = null);
    }

    public class Logger : ILogger
    {
        private readonly ILog _logger;
        public Logger()
        {
            this._logger = LogManager.GetLogger(typeof(Logger));
        }
        public void Debug(string message)
        {
            this._logger?.Debug(message);
        }
        public void Info(string message)
        {
            this._logger?.Info(message);
        }
        public void Warn(string message, Exception? ex = null)
        {
            this._logger?.Warn(message, ex);
        }

        public void Error(string message, Exception? ex = null)
        {
            this._logger?.Error(message, ex);
        }
    }
}