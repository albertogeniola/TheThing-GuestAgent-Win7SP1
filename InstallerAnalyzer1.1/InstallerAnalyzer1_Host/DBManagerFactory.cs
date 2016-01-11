using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace InstallerAnalyzer1_Host
{
    /// <summary>
    /// This class aims to create DBManager objects. Factory pattern.
    /// </summary>
    public class DBManagerFactory
    {
        private static readonly string INSERT_NEW_JOB_STATEMENT = "INSERT INTO jobs(path, hash) VALUES(@PATH, @HASH)";
        private static readonly string SELECT_JOB_BY_HASH_STATEMENT = "SELECT id, path, hash, insertDate, startDate, finishDate, finished FROM jobs WHERE hash = @HASH";
        private static readonly string INSERT_TODO_STATEMENT = "INSERT INTO todo(jId, controlPath) VALUES(@JID,@CONTROLPATH)";
        private static readonly string SELECT_JOB_UNFINISHED_STATEMENT = "SELECT J.id, J.path, J.hash, J.insertDate, J.startDate, J.finishDate, J.finished, T.id, T.jid, T.controlPath FROM todo as T, jobs as J WHERE J.finished = 0 AND T.jid = J.id AND T.inProgress=0 LIMIT 1";
        private static readonly string SET_IN_PROGRESS_TODO_BY_ID_STATEMENT = "UPDATE todo SET inProgress=1 WHERE id=@ID";
        private static readonly string DELETE_TODO_BY_ID_STATEMENT = "DELETE FROM todo WHERE id=@ID";
        private static readonly string INERT_ONE_TODO_IN_PROGRESS_STATEMENT = "INSERT INTO todo(jId,controlPath,inProgress) VALUES(@JID,@CONTROLPATH,1)";
        private static readonly string INERT_DONE_JOB_STATEMENT = "INSERT INTO done(jId,controlPath,result) VALUES(@JID,@CONTROLPATH,@RESULT)";
        private static readonly string IS_JOB_DONE_OR_TODO_STATEMENT = "(SELECT T.id FROM todo AS T WHERE T.jId=@JID AND T.controlPath=@CONTROLPATH) UNION (SELECT D.id FROM done as D WHERE D.controlPath=@CONTROLPATH AND D.jId=@JID) LIMIT 1";
        private static readonly string DELETE_TODO_BY_STARTING_CONTROLPATH_STATEMENT = "DELETE FROM todo WHERE controlPath like @CONTROLPATH";
        private static readonly string SELECT_TODO_OR_DONE_BY_CONTROLPATH_STATEMENT = "SELECT * FROM todo as T, done as D WHERE T.controlPath=@CONTROLPATH OR D.controlPath=@CONTROLPATH";
        private static readonly string SELECT_STATICS = "SELECT (SELECT COUNT(*) FROM test.todo) As Remaining, (SELECT COUNT(*) FROM test.done) As Done";
        private static readonly string INERT_DONE_WITH_ERROR_JOB_STATEMENT = "INSERT INTO done(jId,controlPath,result,error) VALUES(@JID,@CONTROLPATH,@RESULT,@ERROR)";

        
        private static readonly string DEFAULT_TODO_PATH = "appstart\\";

        public static DBManager NewManager()
        {
            return new MySqlDbManager(InstallerAnalyzer1_Host.Properties.Resources.MySqlConnectionString);
        }

        /// <summary>
        /// Class with limited scope: implements the DBManager interface using a MySql Database
        /// </summary>
        class MySqlDbManager : DBManager
        {
            private string _connString;
            private MySqlConnection _conn;

            public MySqlDbManager(string connectionString)
            {
                _connString = connectionString;
                _conn = null;
            }
            private bool ConnectDb()
            {
                if (_conn != null && _conn.State==System.Data.ConnectionState.Open)
                    return true;

                try
                {
                    _conn = new MySqlConnection(_connString);
                    _conn.Open();
                    return true;
                }
                catch (MySqlException ex)
                {
                    Log("Error during the connection to the database: "+ex.Message);
                    return false;
                }
            }

            private void Log(string text)
            {
                lock (Console.Out)
                {
                    Console.WriteLine(text);
                }
            }

            private void DisconnectDb()
            {
                if (_conn != null)
                    _conn.Close();
            }
            void IDisposable.Dispose()
            { 
                // If the connection is open, release it
                DisconnectDb();
                _conn.Dispose();
            }

            void DBManager.InsertDoneWithError(string followedPath, Job mainJob,string xmlError)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                using (MySqlCommand cmd = new MySqlCommand(INERT_DONE_WITH_ERROR_JOB_STATEMENT, _conn))
                {
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@JID", mainJob.Id);
                    cmd.Parameters.AddWithValue("@CONTROLPATH", followedPath);
                    cmd.Parameters.AddWithValue("@RESULT", xmlError);
                    cmd.Parameters.AddWithValue("@ERROR", true);
                    int res = cmd.ExecuteNonQuery();

                    if (res != 1)
                    {
                        throw new DBException("I was unable to insert the new TODOJob into the TODO table.");
                    }
                }
            }

            // Functions to speak with DB
            /// <summary>
            /// This function simply add a new record to the JOB table and initialize the TODO_TABLE with STARTAPP record.
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="fileHash"></param>
            /// <exception cref="JobAlreadySubmittedException">If there already is an executable with same binary-hash.</exception>
            /// <exception cref="DBExcetpion">If there's a DB problem.</exception>
            void DBManager.InsertNewJob(string filePath, string fileHash)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }


                // Record already present?
                bool duplicate = false;
                int duplicateId = -1;
                MySqlCommand cmd = new MySqlCommand(SELECT_JOB_BY_HASH_STATEMENT, _conn);
                try{
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@HASH", fileHash.ToString());
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            duplicate = true;
                            duplicateId = reader.GetInt32(0);
                        }
                        else
                        {
                            // That executable has already been inserted into DB
                            duplicate = false;
                        }
                    }
                }
                finally{
                    cmd.Dispose();
                }

                // If found a duplicate, throw an exception!
                if (duplicate)
                    throw new JobAlreadySubmittedException(duplicateId);

                // Otherwise, proceed with insert.
                // Using prepared statements for avoiding injections... there's the overhead downside however...
                int res = 0;
                long jId = -1;
                
                MySqlCommand icmd = new MySqlCommand(INSERT_NEW_JOB_STATEMENT, _conn);
                try
                {
                    icmd.Prepare();
                    icmd.Parameters.AddWithValue("@PATH",filePath);
                    icmd.Parameters.AddWithValue("@HASH",fileHash);
                    res = icmd.ExecuteNonQuery();
                    jId=icmd.LastInsertedId;
                    // Insert the default first action into the TODO table
                    ((DBManager)this).InsertAction(jId, DEFAULT_TODO_PATH);
                }
                finally{
                    icmd.Dispose();
                }
                
            }

            void DBManager.GetTodoAndDone(out int todo, out int done)
            { 
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                using (MySqlCommand cmd = new MySqlCommand(SELECT_STATICS, _conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new DBException("Cannot execute stat query on DB.");
                        }
                        todo = reader.GetInt32("Remaining");
                        done = reader.GetInt32("Done");
                    }
                }
                
                
            }

            /// <summary>
            /// This function cheks the DB and returns true if the path is not yet evaluated (isn't done and isn't in todo)
            /// </summary>
            /// <param name="jId"></param>
            /// <param name="fullPath"></param>
            /// <returns></returns>
            bool DBManager.IsAlreadyDoneOrTodo(int jId, string fullPath)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                object res = null;
                using (MySqlCommand cmd = new MySqlCommand(IS_JOB_DONE_OR_TODO_STATEMENT, _conn))
                {
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@JID", jId);
                    cmd.Parameters.AddWithValue("@CONTROLPATH", fullPath);
                    res = cmd.ExecuteScalar();                  
                }
                if (res == null)
                    // Not found, the job isn't into the db
                    return false;
                else
                    // Found at least one record!
                    return true;
            }

            /// <summary>
            /// Inserts an action to perform into the TODO table, given the Job ID and the Path to execute next time.
            /// </summary>
            /// <param name="jId"></param>
            /// <param name="controlPath"></param>
            void DBManager.InsertAction(long jId, string controlPath)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                int res = 0;
                long todoId = -1;
                using (MySqlCommand cmd = new MySqlCommand(INSERT_TODO_STATEMENT, _conn))
                {
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@JID", jId);
                    cmd.Parameters.AddWithValue("@CONTROLPATH", controlPath);
                    res = cmd.ExecuteNonQuery();
                    todoId = cmd.LastInsertedId;
                }

            }
            /// <summary>
            /// Retrives the first incomplete job from the table. If none is found, returns null
            /// </summary>
            /// <returns></returns>
            TodoJob DBManager.PopTodoJob(out Job mainJob)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                using (MySqlCommand cmd = new MySqlCommand(SELECT_JOB_UNFINISHED_STATEMENT, _conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            mainJob = null;
                            return null;
                        }
                        else
                        {
                            //"SELECT J.id, J.path, J.hash, J.insertDate, J.startDate, J.finishDate, J.finished, T.id, T.jid, T.controlPath FROM todo as T, jobs as J WHERE J.finished = 0 AND T.jid = J.id AND T.inProgress=0 LIMIT 1";
                            int id = reader.GetInt32(0);
                            string path = reader.GetString(1);
                            string hash = reader.GetString(2);
                            DateTime insertDate = reader.GetDateTime(3);

                            DateTime? startDate = null;

                            if (!reader.IsDBNull(4))
                                startDate = reader.GetDateTime(4);

                            DateTime? finishedDate = null;
                            if (!reader.IsDBNull(5))
                                finishedDate = reader.GetDateTime(5);

                            bool finished = reader.GetBoolean(6);

                            mainJob = new Job(id, path, hash, insertDate, startDate, finishedDate, finished);

                            // Fill in the todojob
                            string controlPath = reader.GetString(9);
                            int todoId = reader.GetInt32(7);
                            
                            reader.Close();
                            
                            // Now mark that job as inProgress
                            using (MySqlCommand cmd2 = new MySqlCommand(SET_IN_PROGRESS_TODO_BY_ID_STATEMENT, _conn))
                            {
                                cmd2.Prepare();
                                cmd2.Parameters.AddWithValue("@ID", todoId);
                                int res = cmd2.ExecuteNonQuery();

                                if (res != 1)
                                {
                                    // Update has gone wrong...
                                    throw new DBException("I was unable to update (mark inProgress=1) the record with ID " + todoId + " in table todo");
                                }
                                else
                                {
                                    return new TodoJob(todoId, controlPath);
                                }
                            }
                        }
                    }                    
                }
            }
            
            TodoJob DBManager.InsertActionInProgress(int jId, string controlPath)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                using (MySqlCommand cmd = new MySqlCommand(INERT_ONE_TODO_IN_PROGRESS_STATEMENT, _conn))
                {
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@JID", jId);
                    cmd.Parameters.AddWithValue("@CONTROLPATH", controlPath);
                    int res = cmd.ExecuteNonQuery();
                    
                    if (res!=1)
                    {
                        throw new DBException("I was unable to insert the new TODOJob into the TODO table.");
                    }
                    else
                    {
                        return new TodoJob((int)cmd.LastInsertedId, controlPath);
                    }
                }
                
            }

            void DBManager.DeleteTodoJob(TodoJob done)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                using (MySqlCommand cmd = new MySqlCommand(DELETE_TODO_BY_ID_STATEMENT, _conn))
                {
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@ID", done.Id);
                    int res = cmd.ExecuteNonQuery();
                    //Log("Deleted " + res + " rows.");
                    // I don't check for errors.
                }
            }

            void DBManager.DeleteTodoJobsStartingWithPath(string followedPath)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                using (MySqlCommand cmd = new MySqlCommand(DELETE_TODO_BY_STARTING_CONTROLPATH_STATEMENT, _conn))
                {
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@CONTROLPATH", followedPath+"%");
                    int res = cmd.ExecuteNonQuery();
                    //Log("Deleted "+res+" rows.");
                    // I don't check for errors.
                }
            }

            List<string> DBManager.FilterDoneOrTodoByPath(List<string> allPossibleInteractions)
            {
                List<string> result = new List<string>();

                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }
                
                // Using prepared statements for avoiding injections... there's the overhead downside however...
                using (MySqlCommand cmd = new MySqlCommand(SELECT_TODO_OR_DONE_BY_CONTROLPATH_STATEMENT, _conn))
                {
                    cmd.Prepare();
                    cmd.Parameters.Add("@CONTROLPATH", MySqlDbType.VarChar,2048);
                    foreach (string s in allPossibleInteractions)
                    {
                        cmd.Parameters["@CONTROLPATH"].Value = s;
                        
                        MySqlDataReader res = cmd.ExecuteReader();
                        if (!res.Read())
                            result.Add(s);
                        res.Close();
                    }
                }
                

                return result;
            }

            /// <summary>
            /// Inserts a done TODO-JOB into the DONE table. 
            /// </summary>
            /// <param name="finalPath"></param>
            /// <param name="job"></param>
            /// <param name="xmlResult"></param>
            void DBManager.InsertDone(string finalPath, Job job, string xmlResult)
            {
                if (!ConnectDb())
                {
                    throw new DBException("I was unable to connect to the DB.");
                }

                // Using prepared statements for avoiding injections... there's the overhead downside however...
                using (MySqlCommand cmd = new MySqlCommand(INERT_DONE_JOB_STATEMENT, _conn))
                {
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@JID", job.Id);
                    cmd.Parameters.AddWithValue("@CONTROLPATH", finalPath);
                    cmd.Parameters.AddWithValue("@RESULT", xmlResult);
                    int res = cmd.ExecuteNonQuery();

                    if (res != 1)
                    {
                        throw new DBException("I was unable to insert the new TODOJob into the TODO table.");
                    }
                }
            }
        }

    }
}
