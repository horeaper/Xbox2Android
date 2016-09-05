using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Xbox2Android.Controls
{
	interface IDisplayControl : IInputElement
	{
		bool IsSelected { get; set; }
		Point Location { get; set; }
	}
}
