using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace todoBiometric.Functions.Entities
{
    public class BiometricClockEntities : TableEntity
    {
        public int Id { get; set; }

        public DateTime createDate { get; set; }

        public int type { get; set; }

        public bool consolidated { get; set; }
    }
}