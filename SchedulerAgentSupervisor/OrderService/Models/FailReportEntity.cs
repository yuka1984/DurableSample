﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace OrderService.Models
{
    public class FailReportEntity : TableEntity
    {
        public string Json { get; set; }
    }
}
