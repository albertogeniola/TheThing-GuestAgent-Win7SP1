using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace InstallerAnalyzer1_Host
{
    public class Stats
    {

        // Singleton
        private static Stats _instance;
        public static Stats Instance
        {
            get {
                if (_instance == null)
                    _instance = new Stats();
                return _instance;
            }
        }

        private int _pathsCompleted = 0;
        private int _loops = 0;
        private Timer _timer;
        private static string pathsCompletedLabel = "Paths Completed: ";
        private static string loopsLabel = "Loops Occurred: ";
        private static string timeLapsedLabel = "Time Lapsed: {0:D2}:{1:D2}:{2:D2}";
        private static string averagePathsPerMinuteLabel = "Avg Paths/Minute: ";
        private static string averageLoopsPerMinuteLabel = "Avg Loops/Minute: ";
        private static string statsLabel = "Todo/Done: ";
        
        private static DBManager _dbm;

        private DateTime _startTime;

        public void Start()
        {
            lock (Console.Out)
            {
                _dbm = DBManagerFactory.NewManager();
                _timer = new Timer();
                _startTime = DateTime.Now;
                _pathsCompleted = 0;
                _timer.Elapsed += new ElapsedEventHandler(Run);
                _timer.Interval = 1000;
                _timer.Enabled = true;
            }
        }

        public void Stop()
        {
            _pathsCompleted = 0;
            _loops = 0;
            _timer.Enabled = false;
            _timer.Close();
        }

        private void Run(object sender, ElapsedEventArgs e)
        {
            lock (Console.Out)
            {
                TimeSpan lapse = DateTime.Now.Subtract(_startTime);
                // Save cursor position
                int prevY = Console.CursorTop;
                int prevX = Console.CursorLeft;
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                
                Console.CursorTop = 0;
                int todo;
                int done;
                _dbm.GetTodoAndDone(out todo, out done);
                
                Console.WriteLine(statsLabel + done + "/"+ todo);

                Console.WriteLine(pathsCompletedLabel+_pathsCompleted+" - "+loopsLabel+_loops);
                Console.WriteLine(timeLapsedLabel,(int)lapse.TotalHours,lapse.Minutes,lapse.Seconds);
                double avgPaths = _pathsCompleted / lapse.TotalMinutes;
                double avgLoops = _loops / lapse.TotalMinutes;
                Console.WriteLine(averagePathsPerMinuteLabel+avgPaths);
                Console.WriteLine(averageLoopsPerMinuteLabel + avgLoops);
                Console.CursorTop = prevY;
                Console.CursorLeft = prevX;
                Console.ResetColor();
            }
        }

        public void notifyCompletedPath()
        {
            lock (Console.Out)
            {
                _pathsCompleted++;
            }
        }


        public void notifyLoop()
        {
            lock (Console.Out)
            {
                _loops++;
            }
        }
    }
}
