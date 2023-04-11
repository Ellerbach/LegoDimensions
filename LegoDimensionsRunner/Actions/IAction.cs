using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensionsRunner.Actions
{
    public interface IAction
    {
        string Name { get; }

        int? Duration { get; set; }
    }
}
