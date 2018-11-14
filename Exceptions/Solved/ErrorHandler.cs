using System;
using NLog;

namespace Exceptions.Solved
{
    public static class ErrorHandler
    {
        private static readonly Logger log = LogManager.GetLogger("global");
        public static void LogErrors(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        public static T Refine<T>(Func<T> func,
            Func<Exception, Exception> createRefinedError)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                throw createRefinedError(e);
            }
        }

    }
}