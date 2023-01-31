using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    public class Status
    {
        public static readonly Status Success = new Status(StatusCode.Ok, "");
        public static readonly Status Failed = new Status(StatusCode.Failed, "");

        public StatusCode Code { get; set; }
        public string Message { get; set; }

        public Status() { }

        public Status(StatusCode code, string message)
        {
            Code = code;
            Message = message;
        }

        public bool IsOk() => Code == StatusCode.Ok;
    }

}
