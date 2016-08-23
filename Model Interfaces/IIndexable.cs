using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PsqlAdapter.Model_Interfaces
{
    public interface IIndexable
    {
        [Key]
        int Id { get; set; }
    }
}
