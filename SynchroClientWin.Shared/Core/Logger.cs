using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchroCore
{
    // There are appropriate static public interfaces provided below to do logging configuration (default, and per class).
    //
    // To set the default log level given a "defaultLevel" from a configuration, you would do:
    //
    //     Logger.DefaultLogLevel = LogLevel.FromString(defaultLevel);
    //
    // To set a specific level for a class using a "className" and "classLevel" from a configuration, you would do:
    //
    //     Logger.GetLogger(className).Level = LogLevel.FromString(classLevel);
    //

    public class LogLevel
    {
        private readonly int ordinal;
        private readonly string name;

        private LogLevel(string name, int ordinal)
        {
            this.name = name;
            this.ordinal = ordinal;
        }

        public string Name
        {
            get { return this.name; }
        }

        public int Ordinal
        {
            get { return this.ordinal; }
        }

        public override string ToString()
        {
            return this.Name;
        }

        public static readonly LogLevel Trace = new LogLevel("Trace", 0);
        public static readonly LogLevel Debug = new LogLevel("Debug", 1);
        public static readonly LogLevel Info  = new LogLevel("Info", 2);
        public static readonly LogLevel Warn  = new LogLevel("Warn", 3);
        public static readonly LogLevel Error = new LogLevel("Error", 4);
        public static readonly LogLevel Fatal = new LogLevel("Fatal", 5);
        public static readonly LogLevel Off   = new LogLevel("Off", 6);

        private static readonly LogLevel[] _levels = new LogLevel[] { Trace, Debug, Info, Warn, Error, Fatal, Off };

        public static LogLevel FromString(string levelName)
        {
            if (levelName == null)
            {
                throw new ArgumentNullException("levelName");
            }

            foreach (LogLevel level in _levels)
            {
                if (levelName.Equals(level.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return level;
                }
            }

            throw new ArgumentException("Unknown log level: " + levelName);
        }
    }
     
    public class Logger
    {
        // This is our static Logger "factory"
        //
        private static Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();

        public static Logger GetLogger(string className)
        {
            if (_loggers.ContainsKey(className))
            {
                return _loggers[className];
            }
            else
            {
                Logger logger = new Logger(className);
                _loggers.Add(className, logger);
                return logger;
            }
        }

        private static LogLevel _defaultLogLevel = LogLevel.Info;
        public static LogLevel DefaultLogLevel { get { return _defaultLogLevel; } set { _defaultLogLevel = value; } }

        // This is our Logger instance implementation
        //
        private readonly string _className;
        private LogLevel _level = null;

        private Logger(string className)
        {
            this._className = className;
        }

        public LogLevel Level 
        { 
            get 
            { 
                if (this._level == null)
                {
                    return _defaultLogLevel;
                }
                return this._level; 
            } 
            set 
            { 
                this._level = value; 
            } 
        }

        public void Log(LogLevel level, string format, params object[] args)
        {
            if (level.Ordinal >= this.Level.Ordinal)
            {
                string time = System.DateTime.Now.ToString("yyyy-mm-dd hh:mm:ss.fff");
                string logEventDetails = string.Format(format, args);
                string logEventFormat = "[{0}] [{1}] {2} - {3}";
                string logEvent = string.Format(logEventFormat, time, level.Name, this._className, logEventDetails);
                System.Diagnostics.Debug.WriteLine(logEvent);
            }
        }

        public void Trace(string format, params object[] args)
        {
            Log(LogLevel.Trace, format, args);
        }

        public void Debug(string format, params object[] args)
        {
            Log(LogLevel.Debug, format, args);
        }

        public void Info(string format, params object[] args)
        {
            Log(LogLevel.Info, format, args);
        }

        public void Warn(string format, params object[] args)
        {
            Log(LogLevel.Warn, format, args);
        }

        public void Error(string format, params object[] args)
        {
            Log(LogLevel.Error, format, args);
        }

        public void Fatal(string format, params object[] args)
        {
            Log(LogLevel.Fatal, format, args);
        }
    }
}
