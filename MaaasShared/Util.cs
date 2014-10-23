using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public static class Util
    {
        // DontWait is used to wrap calls to async methods from non-async methods where you expicitly
        // do not want to wait for the async method (task) to complete.  Using the DontWait wrapper is
        // a way of comminicating that the method being called is async and that any code following the
        // call will not wait until it is completed, and that this was an explicit choice.
        //
        // In Windows parlance, the DontWait wrapper makes the call of the method that produced the 
        // Task "fire and forget".
        //
        public static void DontWait(Task t)
        {
            return;
        }
    }
}
