using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerAnalyzer1_Host
{
    public interface DBManager:IDisposable
    {
        void InsertNewJob(string filePath, string fileHash);
        void InsertAction(long jId, string controlPath);
        
        /// <summary>
        /// This method will pop the TODO job from the DB. It means the record is read, marked as 'IN PROGRESS' and returned. If no more jobs are found, returns NULL
        /// </summary>
        /// <param name="incompleteJob"></param>
        /// <returns></returns>
        TodoJob PopTodoJob(out Job incompleteJob);
        void DeleteTodoJob(TodoJob done);

        void GetTodoAndDone(out int todo, out int done);

        /// <summary>
        /// Inserts a TODOJOB into the relative table and gives back the relative todo job item.
        /// </summary>
        /// <param name="jId"></param>
        /// <param name="controlPath"></param>
        /// <returns></returns>
        TodoJob InsertActionInProgress(int jId, string controlPath);

        void InsertDone(string finalPath, Job job, string xmlResult);

        bool IsAlreadyDoneOrTodo(int p, string s);

        void DeleteTodoJobsStartingWithPath(string followedPath);

        List<string> FilterDoneOrTodoByPath(List<string> allPossibleInteractions);

        void InsertDoneWithError(string p, Job _mainJob, string xmlErrorLog);
    }
}
