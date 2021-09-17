using System.Collections.Generic;
using System.Linq;

namespace MaxFlowOptimizeDemo.Utils
{
    /// <summary>
    /// Utility class to store all needed deconstructor methods
    /// </summary>
    public static class Deconstrutors
    {
        /// <summary>
        /// List deconstructor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="head"></param>
        /// <param name="tail"></param>
        public static void Deconstruct<T>(this List<T> list, out T head, out List<T> tail)
        {
            head = list.FirstOrDefault();
            tail = new List<T>(list.Skip(1));
        }
    }
}
