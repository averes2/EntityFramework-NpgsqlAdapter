using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PsqlAdapter.Model_Interfaces
{
    public interface INameable
    {
        [Key]
        string Name { get; set; }
    }
}
