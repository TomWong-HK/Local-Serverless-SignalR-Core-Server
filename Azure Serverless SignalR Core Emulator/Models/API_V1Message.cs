using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Azure_Serverless_SignalR_Core_Emulator.Models
{
    public class API_V1Message
    {
        [Required]
        public string target { get; set; }
        [Required]

        public object[] arguments { get; set; }
    }
}
