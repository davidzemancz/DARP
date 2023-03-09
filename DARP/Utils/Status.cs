using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    /// <summary>
    /// Status
    /// </summary>
    public class Status
    {
        /// <summary>
        /// Success status
        /// </summary>
        public static readonly Status Success = new Status(StatusCode.Ok, "");

        /// <summary>
        /// Fail status
        /// </summary>
        public static readonly Status Failed = new Status(StatusCode.Failed, "");

        /// <summary>
        /// Status code
        /// </summary>
        public StatusCode Code { get; set; }

        /// <summary>
        /// Status message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Initialize new status
        /// </summary>
        /// <param name="code">Code</param>
        /// <param name="message">Message</param>
        public Status(StatusCode code, string message)
        {
            Code = code;
            Message = message;
        }
    }

}
