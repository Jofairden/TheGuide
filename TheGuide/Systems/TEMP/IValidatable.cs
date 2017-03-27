using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheGuide.Systems.TEMP
{
    public interface IValidatable
    {
	    void SetDefault();
	    bool Validate();
    }
}
