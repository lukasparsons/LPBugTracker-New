﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LPBugTracker.Models
{
    public class TicketNotification
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string UserId { get; set; }

        public string Message { get; set; }
        public DateTime Created { get; set; }
        public bool Read { get; set; }

        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}