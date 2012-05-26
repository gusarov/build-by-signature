using System.Collections.Generic;
using System.Linq;
using System;

namespace MyUtils.UAC
{
	public interface IElevationCall
	{
		string Call(string args);
	}
}