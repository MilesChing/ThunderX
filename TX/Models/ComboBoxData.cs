using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Models
{
    public abstract class ComboBoxData
    {
        protected abstract string FormatText();

        public string Text => FormatText();
    }
}
